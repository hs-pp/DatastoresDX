using System;

namespace DatastoresDX.Editor
{
    // Overrides Workflow type or DataElement type when looking up various editor elements. Mainly used for DataCollections impl.
    public interface ILookupTypeOverride
    {
        Type LookupType { get; }
    }
}