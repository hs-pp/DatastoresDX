using DatastoresDX.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public class DefaultOverviewPanel : AOverviewPanel
    {
        private const string VIEW_UXML = "DatastoresDX/DefaultOverviewPanel";

        private const string WORKFLOW_NAME_LABEL = "workflow-name-label";
        
        private Label m_workflowNameLabel;

        protected override VisualElement CreatePanel()
        {
            VisualElement panel = new VisualElement();
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(panel);
            m_workflowNameLabel = panel.Q<Label>(WORKFLOW_NAME_LABEL);
            return panel;
        }

        protected override void OnSetWorkflow(AWorkflow workflow)
        {
            m_workflowNameLabel.text = workflow.DisplayName;
        }
    }
}