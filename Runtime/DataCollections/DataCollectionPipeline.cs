using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatastoresDX.Runtime.DataCollections
{
    /// <summary>
    /// Generic pipeline for DataCollections.
    /// Because DataCollections use a TemplatePipelineProvider, all DataCollections will go through this Pipeline type.
    /// </summary>
    public class DataCollectionPipeline : APipeline
    {
        public DataCollection DataCollection { get; }
        private Dictionary<Uid, IDataElement> m_elements = new();
        
        public DataCollectionPipeline(DataCollection dataCollection)
        {
            DataCollection = dataCollection;
        }

        public override Task HandleInitialize()
        {
            foreach (IDataElement dataElement in DataCollection.GetAllElements())
            {
                m_elements.Add(dataElement.Id, dataElement);
            }
            return Task.CompletedTask;
        }

        public override IDataElement GetElement(Uid elementId)
        {
            return m_elements[elementId];
        }

        public override List<IDataElement> GetElements()
        {
            return m_elements.Values.ToList();
        }
    }
}
