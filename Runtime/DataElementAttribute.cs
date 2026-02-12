using System;

namespace DatastoresDX.Runtime
{
    /// <summary>
    /// This attribute needs to be included on every DataElement class to provide metadata necessary for Datastores to
    /// register this DataElement type properly.
    /// </summary>
    public class DataElementAttribute : Attribute
    {
        public Type WorkflowType;
        public string CreatePath;
        public string IconPath;

        public DataElementAttribute(Type workflowType, string createPath = "", string iconPath = "")
        {
            WorkflowType = workflowType;
            CreatePath = createPath;
            IconPath = iconPath;
        }
    }
}