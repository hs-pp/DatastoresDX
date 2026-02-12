using System;

namespace DatastoresDX.Runtime
{
    public abstract class ABaseTemplatePipelineProvider : APipelineProvider
    {
        private Uid m_id;
        public override Uid Id => m_id;
        private string m_displayName;
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

        // This is used to determine if the provider should be created. If it returns false, the provider will be destroyed.
        public abstract bool ShouldCreateProvider { get; }
        public abstract void OnSetElementType(Type type);
    }
    
    /// <summary>
    /// Template PipelineProviders are auto created by Datastores for every unique child type of the specified T.
    /// </summary>
    public abstract class ATemplatePipelineProvider<T> : ABaseTemplatePipelineProvider { }
}
