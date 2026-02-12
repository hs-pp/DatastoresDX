using System;
using DatastoresDX.Runtime;

namespace DatastoresDX.Editor
{
    public abstract class ABaseTemplateWorkflowProvider : AWorkflowProvider
    {
        private Uid m_id;
        public override Uid Id => m_id;
        protected string m_displayName;
        public override string DisplayName => m_displayName;
        protected Type m_elementType;
        
        public void SetElementType(Type type)
        {
            m_elementType = type;
            m_id = new Uid(type.FullName.GetHashCode());
            m_displayName = type.Name;
            OnSetElementType(type);
        }

        public Type GetElementType()
        {
            return m_elementType;
        }
        
        public abstract void OnSetElementType(Type type);
    }
    
    public abstract class ATemplateWorkflowProvider<T> : ABaseTemplateWorkflowProvider { }
}