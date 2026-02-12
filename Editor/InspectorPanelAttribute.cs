using System;

namespace DatastoresDX.Editor
{
    public class InspectorPanelAttribute : Attribute
    {
        public Type ElementType;

        public InspectorPanelAttribute(Type elementType)
        {
            ElementType = elementType;
        }
    }
}