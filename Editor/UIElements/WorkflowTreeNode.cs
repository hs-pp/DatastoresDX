using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public class WorkflowTreeNode : VisualElement
    {
        private const string VIEW_UXML = "DatastoresDX/WorkflowTreeNode";
        private const string PROVIDER_ELEMENT_TAG = "provider-element";
        private const string PROVIDER_LABEL_TAG = "provider-label";
        private const string SOLO_WORKFLOW_ELEMENT_TAG = "solo-workflow-element";
        private const string SOLO_NO_WORKFLOW_WARINING_ICON_TAG = "solo-no-workflow-warning-icon";
        private const string SOLO_WORKFLOW_LABEL_TAG = "solo-workflow-label";
        private const string WORKFLOW_ELEMENT_TAG = "workflow-element";
        private const string WORKFLOW_LABEL_TAG = "workflow-label";
        
        private VisualElement m_providerElement;
        private Label m_providerLabel;
        private VisualElement m_soloWorkflowElement;
        private VisualElement m_soloNoWorkflowWarningIcon;
        private Label m_soloWorkflowLabel;
        private VisualElement m_workflowElement;
        private Label m_workflowLabel;

        private DatastoresWindow m_datastoresWindow;
        private BaseWorkflowTreeViewElement m_element;
        
        public WorkflowTreeNode()
        {
            CreateLayout();
            SetupCallbacks();
            Reset();
        }

        public void SetWorkflowWindow(DatastoresWindow datastoresWindow)
        {
            m_datastoresWindow = datastoresWindow;
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);

            m_providerElement = this.Q<VisualElement>(PROVIDER_ELEMENT_TAG);
            m_providerLabel = this.Q<Label>(PROVIDER_LABEL_TAG);
            m_soloWorkflowElement = this.Q<VisualElement>(SOLO_WORKFLOW_ELEMENT_TAG);
            m_soloNoWorkflowWarningIcon = this.Q<VisualElement>(SOLO_NO_WORKFLOW_WARINING_ICON_TAG);
            m_soloWorkflowLabel = this.Q<Label>(SOLO_WORKFLOW_LABEL_TAG);
            m_workflowElement = this.Q<VisualElement>(WORKFLOW_ELEMENT_TAG);
            m_workflowLabel = this.Q<Label>(WORKFLOW_LABEL_TAG);
        }

        private void SetupCallbacks()
        {
            m_providerElement.AddManipulator(new ContextualMenuManipulator(cmpe =>
            {
                cmpe.menu.AppendAction("New Element", dma =>
                {
                    if (m_element is WorkflowProviderTreeViewElement providerElement)
                    {
                        if (providerElement.CreateNewWorkflow() != null)
                        {
                            DatastoresEditorCore.NotifyWorkflowProviderUpdated(providerElement.Provider.Id);
                        }
                    }
                });
            }));
            m_workflowElement.AddManipulator(new ContextualMenuManipulator(cmpe =>
            {
                cmpe.menu.AppendAction("Delete", dma =>
                {
                    if (m_element is WorkflowTreeViewElement workflowElement)
                    {
                        if (workflowElement.DeleteWorkflow())
                        {
                            DatastoresEditorCore.NotifyWorkflowProviderUpdated(workflowElement.Provider.Id);
                            DatastoresEditorCore.NotifyWorkflowUpdated(workflowElement.Workflow.Id);
                        }
                    }
                });
            }));
            m_soloWorkflowElement.AddManipulator(new ContextualMenuManipulator(cmpe =>
            {
                cmpe.menu.AppendAction("Create Element", dma =>
                {
                    if (m_element is SoloWorkflowTreeViewElement soloElement)
                    {
                        if (soloElement.CreateNewWorkflow() != null)
                        {
                            DatastoresEditorCore.NotifyWorkflowProviderUpdated(soloElement.Provider.Id);
                        }
                        else
                        {
                            Debug.LogError("[WorkflowTreeNode] SoloWorkflow already has a workflow.");
                        }
                    }
                });
            }));
            
            var doubleClickWorkflowElement = new Clickable(() =>
            {
                if (m_element is WorkflowTreeViewElement workflowElement)
                {
                    m_datastoresWindow.OpenWorkflow(workflowElement.Workflow.Id);
                }
            });
            doubleClickWorkflowElement.activators.Clear();
            doubleClickWorkflowElement.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            m_workflowElement.AddManipulator(doubleClickWorkflowElement);

            var doubleClickSoloWorkflowElement = new Clickable(() =>
            {
                if (m_element is SoloWorkflowTreeViewElement soloWorkflowElement)
                {
                    if (soloWorkflowElement.Workflow == null)
                    {
                        Debug.LogError("Cannot open SoloWorkflow because it has no workflow!");
                        return;
                    }
                    m_datastoresWindow.OpenWorkflow(soloWorkflowElement.Workflow.Id);
                }
            });
            doubleClickSoloWorkflowElement.activators.Clear();
            doubleClickSoloWorkflowElement.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            m_soloWorkflowElement.AddManipulator(doubleClickSoloWorkflowElement);
        }

        public void BindElement(BaseWorkflowTreeViewElement treeviewElement)
        {
            Reset();
            m_element = treeviewElement;
            if (m_element is WorkflowProviderTreeViewElement)
            {
                SetPipelineElement();
            }
            else if (m_element is WorkflowTreeViewElement)
            {
                SetWorkflowElement();
            }
            else if (m_element is SoloWorkflowTreeViewElement)
            {
                SetSoloWorkflowElement();
            }
        }

        private void Reset()
        {
            ResetPipelineElement();
            ResetSoloWorkflowElement();
            ResetWorkflowElement();
        }
        
        private void SetPipelineElement()
        {
            m_providerElement.style.display = DisplayStyle.Flex;
            m_providerLabel.text = $"[{m_element.DisplayName}]";
        }

        private void SetWorkflowElement()
        {
            m_workflowElement.style.display = DisplayStyle.Flex;
            m_workflowLabel.text = $"{m_element.DisplayName}";
        }
        
        private void SetSoloWorkflowElement()
        {
            m_soloWorkflowElement.style.display = DisplayStyle.Flex;
            m_soloWorkflowLabel.text = $"{m_element.DisplayName}";
            m_soloNoWorkflowWarningIcon.style.display = (m_element as SoloWorkflowTreeViewElement).Workflow == null ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ResetPipelineElement()
        {
            m_providerElement.style.display = DisplayStyle.None;
            m_providerLabel.text = "";
            m_providerLabel.Unbind();
        }
        
        private void ResetSoloWorkflowElement()
        {
            m_soloWorkflowElement.style.display = DisplayStyle.None;
            m_soloWorkflowLabel.text = "";
        }
        
        private void ResetWorkflowElement()
        {
            m_workflowElement.style.display = DisplayStyle.None;
            m_workflowLabel.text = "";
            m_workflowLabel.Unbind();
        }
    }
}