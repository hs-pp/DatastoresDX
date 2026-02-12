using System;

namespace DatastoresDX.Editor
{
    /// <summary>
    /// Attribute to attach to ElementTreeNodeDrawer classes to provide metadata necessary for Datastores to register
    /// the TreeNodeDrawer properly.
    /// </summary>
    public class ElementTreeNodeDrawerAttribute : Attribute
    {
        public Type ElementType;

        public ElementTreeNodeDrawerAttribute(Type elementType)
        {
            ElementType = elementType;
        }
    }
}