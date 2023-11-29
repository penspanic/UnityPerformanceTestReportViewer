using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PerformanceTestReportViewer.Definition;
using UnityEngine;

namespace PerformanceTestReportViewer
{
    public static class TestInformationGetter
    {
        private static bool initialized = false;
        private static ReadOnlyDictionary<string, bool> comparableTestClasses;
        private static ReadOnlyDictionary<string, ISampleDefinition> sampleDefinitions;
        private static ReadOnlyDictionary<string, Func<PerformanceTestResultContext, ISampleDefinition[]>> sampleGroupCategoryFactoryFuncs;
        private static ReadOnlyDictionary<string, List<ISampleDefinition>> sampleDefinitionsByCategory;

        private static Dictionary<PerformanceTestResultContext, ISampleDefinition[]> sampleGroupCategoriesCache = new();
        private static void LazyInit()
        {
            if (initialized)
                return;
            initialized = true;

            // TODO: find smart way to get all classes with ComparableTestAttribute, this way is slow
            var comparableTestClasses_ = new Dictionary<string, bool>();
            var sampleDefinitions_ = new Dictionary<string, ISampleDefinition>();
            var sampleDefinitionFactoryFuncs_ = new Dictionary<string, Func<PerformanceTestResultContext, ISampleDefinition[]>>();
            var sampleDefinitionsByCategory_ = new Dictionary<string, List<ISampleDefinition>>();

            void HandleSampleDefinitionContainer(Type containerType)
            {
                foreach (ISampleDefinition sampleDefinition in containerType.GetFields(BindingFlags.Static | BindingFlags.Public)
                             .Where(f => typeof(ISampleDefinition).IsAssignableFrom(f.FieldType))
                             .Select(f => f.GetValue(null) as ISampleDefinition)
                             .Select(d => d).Where(d => d != null))
                {
                    string definitionName = sampleDefinition.Name;
                    if (sampleDefinitions_.TryAdd($"{sampleDefinition.Category}.{definitionName}", sampleDefinition) == false)
                    {
                        Debug.LogError($"Duplicate class name {containerType.FullName}:{definitionName} with ISampleDefinition!");
                        continue;
                    }

                    AddSampleDefinitionsByCategory(sampleDefinition);
                }

                foreach (ISampleDefinition sampleDefinition in containerType.GetProperties(BindingFlags.Static | BindingFlags.Public)
                             .Where(f => typeof(ISampleDefinition).IsAssignableFrom(f.PropertyType))
                             .Select(f => f.GetValue(null) as ISampleDefinition)
                             .Select(d => d).Where(d => d != null))
                {
                    string definitionName = sampleDefinition.Name;
                    if (sampleDefinitions_.TryAdd($"{sampleDefinition.Category}.{definitionName}", sampleDefinition) == false)
                    {
                        Debug.LogError($"Duplicate class name {containerType.FullName}:{definitionName} with ISampleDefinition!");
                        continue;
                    }

                    AddSampleDefinitionsByCategory(sampleDefinition);
                }
            }

            void AddSampleDefinitionsByCategory(ISampleDefinition definition)
            {
                if (sampleDefinitionsByCategory_.TryGetValue(definition.Category, out List<ISampleDefinition> definitions) == false)
                {
                    definitions = new List<ISampleDefinition>();
                    sampleDefinitionsByCategory_[definition.Category] = definitions;
                }
                definitions.Add(definition);
            }
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.GetCustomAttribute<SampleDefinitionContainerAttribute>() != null)
                    {
                        HandleSampleDefinitionContainer(type);
                    }
                    if (type.GetCustomAttribute<ComparableTestAttribute>() != null)
                    {
                        if (comparableTestClasses_.TryAdd(type.FullName, true) == false)
                        {
                            Debug.LogError($"Duplicate class name {type.FullName} with ComparableTestAttribute!");
                            continue;
                        }
                    }

                    if (typeof(ISampleDefinitionFactory).IsAssignableFrom(type) && type.IsAbstract == false && type.IsInterface == false)
                    {
                        // If it has default constructor, create instance and add to sampleGroupCategories
                        if (type.GetConstructor(Type.EmptyTypes) != null)
                        {
                            ISampleDefinitionFactory instance = (ISampleDefinitionFactory) Activator.CreateInstance(type);
                            if (sampleDefinitionFactoryFuncs_.TryAdd(instance.Category, (PerformanceTestResultContext context) => { return instance.Create(context); }) ==
                                false)
                            {
                                Debug.LogError($"Duplicate class name {type.FullName} with ISampleDefinitionFactory!");
                            }
                        }
                    }
                }
            }

            comparableTestClasses = new ReadOnlyDictionary<string, bool>(comparableTestClasses_);
            sampleDefinitions = new ReadOnlyDictionary<string, ISampleDefinition>(sampleDefinitions_);
            sampleGroupCategoryFactoryFuncs = new ReadOnlyDictionary<string, Func<PerformanceTestResultContext, ISampleDefinition[]>>(sampleDefinitionFactoryFuncs_);
            sampleDefinitionsByCategory = new ReadOnlyDictionary<string, List<ISampleDefinition>>(sampleDefinitionsByCategory_);
        }

        public static void ClearCache()
        {
            sampleGroupCategoriesCache.Clear();
        }

        public static bool IsComparableTest(string className)
        {
            LazyInit();
            return comparableTestClasses.ContainsKey(className);
        }

        public static ISampleDefinition FindSampleGroupDefinition(string sampleGroupName, PerformanceTestResultContext context)
        {
            LazyInit();
            if (PerformanceTestReportViewerUtility.TryParseFromSampleGroupName(sampleGroupName, out string sampleTargetName, out string categoryName, out string definitionName) == false)
                throw new Exception($"Unable to parse sample group name {sampleGroupName}!");

            if (sampleDefinitions.TryGetValue($"{categoryName}.{definitionName}", out ISampleDefinition sampleDefinition))
            {
                return sampleDefinition;
            }

            if (sampleGroupCategoriesCache.TryGetValue(context, out ISampleDefinition[] cachedDefinitions) == false)
            {
                cachedDefinitions = sampleGroupCategoryFactoryFuncs.Values.SelectMany(f => f(context)).ToArray();
                sampleGroupCategoriesCache[context] = cachedDefinitions;
            }


            foreach (ISampleDefinition definition in cachedDefinitions)
            {
                if (definition.Name == definitionName)
                    return definition;
            }

            return null;
        }

        public static string[] GetAllSampleGroupCategories(PerformanceTestResultContext context)
        {
            LazyInit();
            var result = new List<string>(sampleDefinitions.Select(s => s.Value.Category));
            foreach (string category in sampleGroupCategoryFactoryFuncs.Keys)
            {
                result.Add(category);
            }

            return result.Distinct().ToArray();
        }

        public static ISampleDefinition[] GetDefinitionsInCategory(PerformanceTestResultContext context, string category)
        {
            LazyInit();
            if (sampleDefinitionsByCategory.TryGetValue(category, out List<ISampleDefinition> definitions))
            {
                return definitions.ToArray();
            }

            if (sampleGroupCategoryFactoryFuncs.TryGetValue(category, out Func<PerformanceTestResultContext, ISampleDefinition[]> factoryFunc))
            {
                return factoryFunc(context);
            }

            return null;
        }
    }
}