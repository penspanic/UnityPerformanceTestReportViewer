using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.UI;
using Unity.PerformanceTesting.Data;
using UnityEngine;

namespace PerformanceTestReportViewer
{
    [Serializable]
    public class ViewerOptions
    {
        public static readonly int[] ViewDataCountChoices = { 3, 5, 10, 20, 30, 50, 100 };
        public static readonly SortMethod[] SortMethodChoices = { SortMethod.None, SortMethod.Ascending, SortMethod.Descending };
        public static readonly ViewerType[] ViewerTypeChoices = { ViewerType.Bars_Single, ViewerType.Lines_Grouped };

        private Dictionary<(string, string), bool> sampleGroupToggles = new();
        private Dictionary<string, bool> contextToggles = new();
        public int ViewDataCount { get; set; } = ViewDataCountChoices[2];
        public SortMethod SortMethod { get; set; } = SortMethod.None;

        public ViewerType ViewerType { get; set; } = ViewerType.Bars_Single; // TODO: window나 panel별, 혹은 테스트 데이터별 option으로?
        private class JsonSchema
        {
            public List<((string, string), bool)> sampleGroupToggles;
            public string sampleGroupTogglesStr;
            
            public List<(string, bool)> contextToggles;
            public string contextTogglesStr;
            public int viewDataCount;
            public SortMethod sortMethod;
        }

        public string SerializeAsString()
        {
            var bf = new BinaryFormatter();
            using var memory = new MemoryStream();
            bf.Serialize(memory, this);
            return Convert.ToBase64String(memory.GetBuffer());
        }

        public static ViewerOptions DeserializeFromString(string str)
        {
            try
            {
                var bf = new BinaryFormatter();
                using var memory = new MemoryStream(Convert.FromBase64String(str));
                return bf.Deserialize(memory) as ViewerOptions;
            }
            catch (Exception e)
            {
                return new();
            }
        }

        public bool IsOn(SampleGroup sampleGroup)
        {
            if (Utility.TryParseFromSampleGroupName(sampleGroup.Name, out string contextName, out string categoryName, out string definitionName) == false)
                return true;

            return IsOn(contextName, categoryName, definitionName);
        }
        public bool IsOn(string  category, ISampleDefinition definition)
        {
            return IsOn(category, definition.Name);
        }

        public bool IsOn(string category, string definition)
        {
            if (definition == "Time")
                return false;

            if (sampleGroupToggles.TryGetValue((category, definition), out bool value))
                return value;

            return true;
        }

        public bool IsOn(string context, string category, string definition)
        {
            if (IsSampleTargetOn(context) == false)
                return false;

            return IsOn(category, definition);
        }

        public bool IsSampleTargetOn(string context)
        {
            if (contextToggles.Count == 0)
                return true;

            if (contextToggles.TryGetValue(context, out bool value))
                return value;

            return true;
        }

        public void SetIsOn(string category, ISampleDefinition definition, bool value)
        {
            sampleGroupToggles[(category, definition.Name)] = value;
        }

        public void SetIsOnCategory(PerformanceTestResultContext context, string category, bool value)
        {
            foreach (ISampleDefinition groupDefinition in TestInformationGetter.GetDefinitionsInCategory(context, category))
            {
                sampleGroupToggles[(category, groupDefinition.Name)] = value;
            }
        }

        public void SetIsSampleTargetOn(string context, bool value)
        {
            contextToggles[context] = value;
        }

        public bool IsAnythingOn(string category)
        {
            bool allOff = true;
            foreach (var p in sampleGroupToggles.Keys.Where(p => p.Item1 == category))
            {
                if (sampleGroupToggles[p])
                    allOff = false;
            }

            return allOff == false;
        }

        public bool IsEverythingOn(string category)
        {
            foreach (var p in sampleGroupToggles.Keys.Where(p => p.Item1 == category))
            {
                if (sampleGroupToggles[p] == false)
                    return false;
            }

            return true;
        }

        public bool IsEverySampleTargetOn()
        {
            if (contextToggles.Count == 0)
                return true;

            return contextToggles.Values.All(v => v);
        }
    }
}