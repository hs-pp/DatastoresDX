using System;

namespace DatastoresDX.Editor
{
    public class OverviewPanelAttribute : Attribute
    {
        public Type WorkflowType;

        public OverviewPanelAttribute(Type workflowType)
        {
            WorkflowType = workflowType;
        }
    }
}