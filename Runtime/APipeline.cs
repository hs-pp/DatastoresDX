using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatastoresDX.Runtime
{
    public abstract class APipeline
    {
        public Uid Id { get; protected set; }
        public string DisplayName { get; protected set; }
        
        private bool m_isInitialized;
        
        public async Task Initialize()
        {
            if (!m_isInitialized)
            {
                await HandleInitialize();
                m_isInitialized = true;
            }
        }
        
        public abstract Task HandleInitialize();
        public abstract IDataElement GetElement(Uid elementId);
        public abstract List<IDataElement> GetElements();
    }
}