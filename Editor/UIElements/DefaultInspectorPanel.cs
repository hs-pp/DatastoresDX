using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public class DefaultInspectorPanel : AInspectorPanel
    {
        private const string VIEW_UXML = "DatastoresDX/DefaultInspectorPanel";

        private const string ELEMENT_NAME_LABEL = "element-name-label";
        private const string WORKFLOW_NAME_LABEL = "workflow-name-label";
        
        private Label m_elementNameLabel;
        private Label m_workflowNameLabel;
        
        protected override VisualElement CreatePanel()
        {
            VisualElement panel = new VisualElement();
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(panel);
            m_workflowNameLabel = panel.Q<Label>(WORKFLOW_NAME_LABEL);
            m_elementNameLabel = panel.Q<Label>(ELEMENT_NAME_LABEL);
            return panel;
        }

        protected override void OnSetElement(WorkflowElementKey elementKey)
        {
            m_elementNameLabel.text = elementKey.GetElement().DisplayName;
            m_workflowNameLabel.text = elementKey.GetWorkflow().DisplayName;
        }
    }
}