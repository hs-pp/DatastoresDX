using System.Collections.Generic;
using DatastoresDX.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace DatastoresDX.Editor
{
    [UxmlElement]
    public partial class ElementListPanel : VisualElement
    {
        private const string VIEW_UXML = "DatastoresDX/ElementListPanel";

        private const string WORKFLOW_BACK_ELEMENT_TAG = "workflow-back-element";
        private const string SELECTED_WORKFLOW_LABEL = "selected-workflow-label";
        private const string SEARCH_FIELD_TAG = "filter-search-field";
        private const string FILTERS_BUTTON_TAG = "filters-button";
        private const string FILTERING_LABEL_TAG = "filtering-label";
        private const string ELEMENT_TREEVIEW_TAG = "element-treeview";
        private const string TOTAL_ELEMENTS_LABEL_TAG = "total-elements-label";

        private VisualElement m_workflowBackElement;
        private Label m_selectedWorkflowLabel;
        private ToolbarSearchField m_searchField;
        private Button m_filtersButton;
        private Label m_filteringLabel;
        private TreeView m_elementTreeView;
        private Label m_totalElementsLabel;

        private DatastoresWindow m_datastoresWindow;
        private DetailsPanel m_detailsPanel;
        private DatastoresWindowState m_state;
        private AWorkflow m_workflow;
        
        private bool m_filtersAreActive;
        
        public ElementListPanel()
        {
            CreateLayout();
            SetupCallbacks();
        }
        
        public void SetWorkflowWindow(DatastoresWindow datastoresWindow, DetailsPanel detailsPanel)
        {
            m_datastoresWindow = datastoresWindow;
            m_detailsPanel = detailsPanel;
            
            // Hack: Unselect element when we're in overview.
            // There's a unity bug where the SelectionChanged gets called a few frames after every compile.
            // This forces the datastores tab to be set to Inspector every time.
            m_detailsPanel.OnSelectedTabChanged += tab =>
            {
                if (tab == DetailsPanel.SelectedTab.OVERVIEW)
                {
                    m_elementTreeView.ClearSelection();
                }
            };
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);

            m_workflowBackElement = this.Q<VisualElement>(WORKFLOW_BACK_ELEMENT_TAG);
            m_selectedWorkflowLabel = this.Q<Label>(SELECTED_WORKFLOW_LABEL);
            m_searchField = this.Q<ToolbarSearchField>(SEARCH_FIELD_TAG);
            m_filtersButton = this.Q<Button>(FILTERS_BUTTON_TAG);
            m_filteringLabel = this.Q<Label>(FILTERING_LABEL_TAG);
            m_elementTreeView = this.Q<TreeView>(ELEMENT_TREEVIEW_TAG);
            m_totalElementsLabel = this.Q<Label>(TOTAL_ELEMENTS_LABEL_TAG);

            m_searchField.RegisterValueChangedCallback(evt =>
            {
                m_state.SearchFieldValue = evt.newValue;
                LoadListElements();
            });
            m_filtersButton.clicked += () =>
            {
                PopupWindow.Show(m_filtersButton.worldBound, new FiltersPopupContent());
            };
            m_elementTreeView.makeItem = () => new ElementTreeNode();
            m_elementTreeView.bindItem = (element, i) =>
            {
                ((ElementTreeNode)element).BindElementNode(
                    new WorkflowElementKey(m_workflow.Id, m_elementTreeView.GetItemDataForIndex<IDataElement>(i).Id),
                     this);
            };
            m_elementTreeView.unbindItem = (element, i) => ((ElementTreeNode)element).ResetElement();
        }

        private void SetupCallbacks()
        {
            m_elementTreeView.AddManipulator(new ContextualMenuManipulator(cmpe =>
            {
                cmpe.menu.AppendAction("Create Element", menuAction =>
                {
                    ElementTypeSearchWindowProvider searchProvider = ScriptableObject.CreateInstance<ElementTypeSearchWindowProvider>();
                    List<ElementTypeDefinition> elementTypes = m_workflow.GetElementTypes();
                    if (elementTypes == null)
                    {
                        elementTypes = new();
                    }
                    searchProvider.Setup(elementTypes, definition =>
                    {
                        Uid newId = m_workflow.AddElement(definition.ElementType, Uid.Invalid);
                        HandleAfterElementAdd(newId);
                    });
                    SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Mouse.current.position.ReadValue())), searchProvider);
                });
            }));
            m_workflowBackElement.AddManipulator(new Clickable(() =>
            {
                m_datastoresWindow.OpenWorkflow(Uid.Invalid);
            }));

            Clickable labelDoubleClicker = new Clickable(() =>
            {
                if (m_workflow == null)
                {
                    return;
                }
                
                m_workflow.OnPingRequested();
            });
            labelDoubleClicker.activators.Clear();
            labelDoubleClicker.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            m_selectedWorkflowLabel.AddManipulator(labelDoubleClicker);
            
            m_elementTreeView.itemIndexChanged += HandleElementMoved;
            m_elementTreeView.selectionChanged += HandleSelectionChanged;
        }

        private void ResetPanel()
        {
            m_state = null;
            m_workflow = null;

            m_selectedWorkflowLabel.text = "-";
            
            m_elementTreeView.selectionChanged -= HandleSelectionChanged;
            m_elementTreeView.SetRootItems(new List<TreeViewItemData<IDataElement>>());
            m_elementTreeView.Rebuild();
            m_elementTreeView.ClearSelection();
            m_elementTreeView.selectionChanged += HandleSelectionChanged;
            
            m_searchField.SetValueWithoutNotify("");
            // TODO: Clear filters
            EvaluateFiltersAreActive();
            
            m_detailsPanel.OpenWorkflow(null);
        }

        public void OpenWorkflow(DatastoresWindowState state)
        {
            m_state = state;
            m_workflow = m_state != null ? DatastoresEditorCore.GetWorkflow(m_state.SelectedWorkflowId) : null;
            style.display = (m_workflow != null) ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (m_workflow == null)
            {
                ResetPanel();
                return;
            }
            
            m_selectedWorkflowLabel.text = m_workflow.DisplayName;
            m_elementTreeView.reorderable = m_workflow.CanMoveElements;
            
            m_searchField.value = m_state.SearchFieldValue;

            LoadListElements();
            
            m_detailsPanel.OpenWorkflow(state);
        }

        private void LoadListElements()
        {
            EvaluateFiltersAreActive();
            if (m_filtersAreActive)
            {
                m_elementTreeView.SetRootItems(GetFilteredTreeViewItemDataList());
            }
            else
            {
                m_elementTreeView.SetRootItems(m_workflow.GetTreeViewRootItems());
            }
            
            m_elementTreeView.selectionChanged -= HandleSelectionChanged;
            m_elementTreeView.Rebuild();
            m_elementTreeView.selectionChanged += HandleSelectionChanged;
            
            // Recover selected element if we're not filtering
            if (!m_filtersAreActive && !m_state.SelectedElementId.IsInvalid())
            {
                m_elementTreeView.SetSelectionByIdWithoutNotify(new[] { m_workflow.GetTreeViewId(m_state.SelectedElementId) });
            }
            m_totalElementsLabel.text = $"{m_elementTreeView.itemsSource.Count} TOTAL ELEMENTS";
        }

        private List<TreeViewItemData<IDataElement>> GetFilteredTreeViewItemDataList()
        {
            List<TreeViewItemData<IDataElement>> treeViewItems =  m_workflow.GetTreeViewRootItems();
            treeViewItems.Reverse();
            
            List<TreeViewItemData<IDataElement>> toReturn = new();
            int treeViewIdCounter = 0;
            
            Stack<TreeViewItemData<IDataElement>> stack = new Stack<TreeViewItemData<IDataElement>>();
            foreach(TreeViewItemData<IDataElement> item in treeViewItems)
            {
                stack.Push(item);
            }

            while (stack.Count != 0)
            {
                TreeViewItemData<IDataElement> item = stack.Pop();
                if (item.children != null)
                {
                    foreach (TreeViewItemData<IDataElement> child in item.children)
                    {
                        stack.Push(child);
                    }
                }
                if (DatastoresEditorUtils.DataElementPassesSearchString(item.data,m_searchField.value))
                {
                    toReturn.Add(new TreeViewItemData<IDataElement>(treeViewIdCounter++, item.data));
                }
            }

            return toReturn;
        }

        public void HandleAfterElementAdd(Uid newId)
        {
            m_state.SelectedElementId = newId;
            m_detailsPanel.SelectTab(DetailsPanel.SelectedTab.INSPECTOR); // Gotta set this manually because the selection changed event doesn't fire when we set the selection manually
            DatastoresEditorCore.NotifyWorkflowUpdated(m_workflow.Id);
        }

        public void HandleElementDelete(Uid elementToDelete)
        {
            if (elementToDelete.Equals(m_state.SelectedElementId))
            {
                m_state.SelectedElementId = Uid.Invalid;
            }
            DatastoresEditorCore.NotifyWorkflowUpdated(m_workflow.Id);
        }
        
        private void HandleElementMoved(int moveTreeViewId, int newParentTreeViewId)
        {
            if (m_state == null)
            {
                return;
            }
            
            //_elementTreeView.viewController.RebuildTree();
            if (m_workflow.CanMoveElements)
            {
                Uid moveId = m_elementTreeView.GetItemDataForId<IDataElement>(moveTreeViewId).Id;
                Uid newParentId = Uid.Invalid;
                if (newParentTreeViewId != -1)
                {
                    newParentId = m_elementTreeView.GetItemDataForId<IDataElement>(newParentTreeViewId).Id;
                }
                int childIndex = m_elementTreeView.viewController.GetChildIndexForId(moveTreeViewId);
                m_workflow.MoveElement(moveId, newParentId, childIndex);
                DatastoresEditorCore.NotifyWorkflowUpdated(m_workflow.Id);
            }
        }
        
        private void HandleSelectionChanged(IEnumerable<object> objs)
        {
            if (m_state == null)
            {
                return;
            }
         
            IDataElement element = (IDataElement)m_elementTreeView.selectedItem;
            m_detailsPanel.SelectElement(element);
            m_state.SelectedElementId = element == null ? Uid.Invalid : element.Id;
        }
        
        private void EvaluateFiltersAreActive()
        {
            m_filtersAreActive = m_state != null && !string.IsNullOrEmpty(m_state.SearchFieldValue);
            m_filteringLabel.style.display = m_filtersAreActive ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}