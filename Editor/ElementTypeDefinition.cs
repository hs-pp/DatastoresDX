using System;
using System.Collections.Generic;

namespace DatastoresDX.Editor
{
    public class ElementTypeDefinition
    {
        public string SearchWindowPath;
        public Type ElementType;
    }
    
    public class ElementTypeDefinitionComparer : IComparer<ElementTypeDefinition>
    {
        public int Compare(ElementTypeDefinition x, ElementTypeDefinition y)
        {
            return String.Compare(x.SearchWindowPath, y.SearchWindowPath, StringComparison.Ordinal);
        }
    }
}