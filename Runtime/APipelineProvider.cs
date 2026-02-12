using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatastoresDX.Runtime
{
    /// <summary>
    /// Base PipelineProvider class to be implemented by any data stream.
    /// This class provides the collection of pipelines that can load collections of game data.
    /// </summary>
    public abstract class APipelineProvider
    {
        public abstract Uid Id { get; } // This Id must match the Id of the workflow provider for it to register properly.
        public virtual string DisplayName => GetType().Name;
        private List<APipeline> m_pipelines = new();
        
        protected abstract Task<List<APipeline>> LoadPipelines();

        public async Task Initialize()
        {
            m_pipelines.Clear();
            m_pipelines = await LoadPipelines();
            foreach (APipeline pipeline in m_pipelines)
            {
                await pipeline.Initialize();
            }
        }

        public List<APipeline> GetPipelines() 
        {
            return m_pipelines;
        }
    }
}