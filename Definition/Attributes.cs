using System;

namespace PerformanceTestReportViewer.Definition
{
    /// <summary>
    /// Annotation that tests in class are comparable, so that the test result can be displayed in a same view.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ComparableTestAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class SampleDefinitionContainerAttribute : Attribute
    {
        public Type[] Types { get; }
        public SampleDefinitionContainerAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}