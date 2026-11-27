using System;

namespace DatastoresDX.Runtime.DataCollections
{
    /// <summary>
    /// Attribute to attach to DataCollection classes to provide metadata necessary for Datastores to register
    /// the DataCollection properly.
    /// </summary>
    public class DataCollectionAttribute : Attribute
    {
        public bool IsSoloWorkflow;
        public bool RuntimeSupported;
        public string DisplayNameOverride;
        public bool AllowFolders;

        public DataCollectionAttribute(bool isSoloWorkflow, bool runtimeSupported = true, string displayNameOverride = null, bool allowFolders = true)
        {
            IsSoloWorkflow = isSoloWorkflow;
            RuntimeSupported = runtimeSupported;
            DisplayNameOverride = displayNameOverride;
            AllowFolders = allowFolders;
        }
    }
}