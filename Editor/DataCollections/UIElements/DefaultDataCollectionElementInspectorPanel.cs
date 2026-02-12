using DatastoresDX.Runtime.DataCollections;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor.DataCollections
{
    [InspectorPanel(typeof(DataCollectionElement))]
    public class DefaultDataCollectionInspectorPanel : AInspectorPanel
    {
        private PropertyField m_propertyField;

        protected override VisualElement CreatePanel()
        {
            m_propertyField = new PropertyField();
            return m_propertyField;
        }

        protected override void OnSetElement(WorkflowElementKey elementKey)
        {
            DataCollectionElementWrapper element = elementKey.GetElement() as DataCollectionElementWrapper;
            if (element == null)
            {
                return;
            }
            
            m_propertyField.BindProperty(element.ElementSP);
        }
    }
}