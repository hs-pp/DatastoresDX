using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    [UxmlElement]
    public partial class WorkflowListPanel : VisualElement
    {
        private const string VIEW_UXML = "DatastoresDX/WorkflowListPanel";
        private const string EDITOR_FILTER_BUTTON_TAG = "editor-filter-button";
        private const string EDITOR_FILTER_LABEL_TAG = "editor-filter-label";
        private const string RUNTIME_FILTER_BUTTON_TAG = "runtime-filter-button";
        private const string RUNTIME_FILTER_LABEL_TAG = "runtime-filter-label";
        private const string PIPELINE_TREEVIEW_TAG = "pipeline-treeview";

        private VisualElement m_editorFilterButton;
        private Label m_editorFilterLabel;
        private VisualElement m_runtimeFilterButton;
        private Label m_runtimeFilterLabel;
        private TreeView m_treeView;

        private DatastoresWindow m_datastoresWindow;
        private DatastoresWindowState m_state;

        private List<TreeViewItemData<BaseWorkflowTreeViewElement>> m_editorWorkflows = new();
        private List<TreeViewItemData<BaseWorkflowTreeViewElement>> m_runtimeWorkflows = new();

        public WorkflowListPanel()
        {
            CreateLayout();
            Reload();
        }

        public void SetWorkflowWindow(DatastoresWindow datastoresWindow, DatastoresWindowState state)
        {
            m_datastoresWindow = datastoresWindow;
            m_state = state;
            SetActiveTab(m_state.IsShowingEditorTab);
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);

            m_editorFilterButton = this.Q<VisualElement>(EDITOR_FILTER_BUTTON_TAG);
            m_editorFilterLabel = this.Q<Label>(EDITOR_FILTER_LABEL_TAG);
            m_runtimeFilterButton = this.Q<VisualElement>(RUNTIME_FILTER_BUTTON_TAG);
            m_runtimeFilterLabel = this.Q<Label>(RUNTIME_FILTER_LABEL_TAG);
            m_treeView = this.Q<TreeView>(PIPELINE_TREEVIEW_TAG);

            m_editorFilterButton.RegisterCallback<MouseDownEvent>(evt => { SetActiveTab(true); });
            m_runtimeFilterButton.RegisterCallback<MouseDownEvent>(evt => { SetActiveTab(false); });

            m_treeView.makeItem = () =>
            {
                WorkflowTreeNode nodeView = new WorkflowTreeNode();
                nodeView.SetWorkflowWindow(m_datastoresWindow);
                return nodeView;
            };
            m_treeView.bindItem = (element, i) =>
            {
                ((WorkflowTreeNode)element).BindElement(m_treeView.GetItemDataForIndex<BaseWorkflowTreeViewElement>(i));
            };
        }
        
        public void Reload()
        {
            BuildWorkflowsTreeViewItemsLists();
            if (m_state != null)
            {
                SetActiveTab(m_state.IsShowingEditorTab);
            }
            else
            {
                SetActiveTab(true);
            }
        }

        private void BuildWorkflowsTreeViewItemsLists()
        {
            m_editorWorkflows.Clear();
            m_runtimeWorkflows.Clear();

            m_editorWorkflows = BuildTreeViewItems(DatastoresEditorCore.GetEditorOnlyProviders());
            m_runtimeWorkflows = BuildTreeViewItems(DatastoresEditorCore.GetRuntimeAvailableProviders());
        }

        private List<TreeViewItemData<BaseWorkflowTreeViewElement>> BuildTreeViewItems(List<AWorkflowProvider> providers)
        {
            int idCounter = 0;
            List<TreeViewItemData<BaseWorkflowTreeViewElement>> treeViewItems = new();

            foreach (AWorkflowProvider provider in providers)
            {
                List<AWorkflow> workflows = provider.GetWorkflows();
                if (provider.IsSoloWorkflow)
                {
                    if (workflows.Count > 1)
                    {
                        Debug.LogError("[WorkflowListPanel] Solo Workflow has more than one workflow: " + provider.DisplayName);
                    }

                    BaseWorkflowTreeViewElement soloWorkflowTreeViewElement;
                    if (workflows.Count != 0)
                    {
                        soloWorkflowTreeViewElement = new SoloWorkflowTreeViewElement(workflows[0], provider);
                    }
                    else
                    {
                        soloWorkflowTreeViewElement = new SoloWorkflowTreeViewElement(null, provider);
                    }
                    var soloWorkflowItem = new TreeViewItemData<BaseWorkflowTreeViewElement>(idCounter++, soloWorkflowTreeViewElement);
                    treeViewItems.Add(soloWorkflowItem);
                }
                else
                {
                    List<TreeViewItemData<BaseWorkflowTreeViewElement>> childWorkflows = new();
                    foreach (AWorkflow workflow in workflows)
                    {
                        BaseWorkflowTreeViewElement workflowTreeViewElement =
                            new WorkflowTreeViewElement(workflow, provider);
                        childWorkflows.Add(
                            new TreeViewItemData<BaseWorkflowTreeViewElement>(idCounter++, workflowTreeViewElement));
                    }

                    BaseWorkflowTreeViewElement providerTreeViewElement = new WorkflowProviderTreeViewElement(provider);
                    var pipelineTreeViewItem =
                        new TreeViewItemData<BaseWorkflowTreeViewElement>(idCounter++, providerTreeViewElement,
                            childWorkflows);
                    treeViewItems.Add(pipelineTreeViewItem);
                }
            }

            return treeViewItems;
        }
        
        private void SetActiveTab(bool isShowingEditorTab)
        {
            if (m_state != null)
            {
                m_state.IsShowingEditorTab = isShowingEditorTab;
            }

            if (isShowingEditorTab)
            {
                SetFilterStyling(m_editorFilterButton, m_editorFilterLabel, true);
                SetFilterStyling(m_runtimeFilterButton, m_runtimeFilterLabel, false);

                m_treeView.SetRootItems(m_editorWorkflows);
            }
            else
            {
                SetFilterStyling(m_editorFilterButton, m_editorFilterLabel, false);
                SetFilterStyling(m_runtimeFilterButton, m_runtimeFilterLabel, true);

                m_treeView.SetRootItems(m_runtimeWorkflows);
            }

            m_treeView.Rebuild();
        }

        private void SetFilterStyling(VisualElement filterButton, Label filterLabel, bool isActive)
        {
            if (isActive)
            {
                filterButton.style.borderBottomColor = new Color(0.219f, 0.219f, 0.219f);
                filterButton.style.backgroundColor = new Color(0.246f, 0.246f, 0.246f);
                filterLabel.style.color = new Color(0.625f, 0.625f, 0.625f);
            }
            else
            {
                filterButton.style.borderBottomColor = new Color(0.164f, 0.164f, 0.164f);
                filterButton.style.backgroundColor = new Color(0.125f, 0.125f, 0.125f);
                filterLabel.style.color = new Color(0.219f, 0.219f, 0.219f);
            }
        }

        public void ToggleView(bool visible)
        {
            SetActiveTab(m_state.IsShowingEditorTab);
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}