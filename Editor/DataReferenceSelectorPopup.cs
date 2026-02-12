using System;
using System.Collections.Generic;
using System.Linq;
using DatastoresDX.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public class DataReferenceSelectorPopup : PopupWindowContent
    {
        private const string VIEW_UXML = "DatastoresDX/DataReferenceSelectorPopup";
        private const string ERROR_BUTTON_TAG = "error-button";
        private const string CLEAR_BUTTON_TAG = "clear-button";
        private const string SEARCH_FIELD_TAG = "search-field";
        private const string TREEVIEW_TAG = "element-treeview";

        private Button m_errorButton;
        private Button m_clearButton;
        private ToolbarSearchField m_searchField;
        private TreeView m_treeView;
        private Type m_dataElementType;
        private Action<Uid> m_setElementAction;

        public DataReferenceSelectorPopup(Type dataElementType, Action<Uid> setElementAction)
        {
            m_dataElementType = dataElementType;
            m_setElementAction = setElementAction;
        }

        public override void OnGUI(Rect rect) { }
        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 360); // Height is also hardcoded into the UXML.
        }

        public override void OnOpen()
        {
            CreateLayout();
            PopulateTreeView(m_searchField.value);
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(editorWindow.rootVisualElement);

            VisualElement root = editorWindow.rootVisualElement;
            m_errorButton = root.Q<Button>(ERROR_BUTTON_TAG);
            m_clearButton = root.Q<Button>(CLEAR_BUTTON_TAG);
            m_searchField = root.Q<ToolbarSearchField>(SEARCH_FIELD_TAG);
            m_treeView = root.Q<TreeView>(TREEVIEW_TAG);

            m_errorButton.clicked += () =>
            {
                m_setElementAction?.Invoke(new Uid("Some random shit".GetHashCode()));
                editorWindow.Close();
            };
            m_clearButton.clicked += () =>
            {
                m_setElementAction?.Invoke(Uid.Invalid);
                editorWindow.Close();
            };
            m_searchField.RegisterValueChangedCallback((evt =>
            {
                PopulateTreeView(evt.newValue);
                m_treeView.Rebuild();
            }));
            m_treeView.itemsChosen += (objects =>
            {
                if (objects.Any() && objects.First() is DataElementTreeViewNode treeViewNode && treeViewNode.IsSelectable)
                {
                    m_setElementAction?.Invoke(treeViewNode.DataElement.Id);
                    editorWindow.Close();
                }
            });
            
            m_treeView.makeItem = () => { return new TreeViewElement(); };
            m_treeView.bindItem = (element, i) => { (element as TreeViewElement).SetTreeViewNode(m_treeView.GetItemDataForIndex<ITreeViewNode>(i));};
            m_treeView.unbindItem = (element, i) => { (element as TreeViewElement).Reset(); };
        }

        private void PopulateTreeView(string searchString)
        {
            bool isSearching = !string.IsNullOrEmpty(searchString);
            int idCounter = 0;
            List<TreeViewItemData<ITreeViewNode>> tree = new();
            List<TreeViewItemData<ITreeViewNode>> workflowTreeViewItems = new();
            List<TreeViewItemData<ITreeViewNode>> dataElementsTreeViewItems = new();
            foreach (AWorkflowProvider provider in DatastoresEditorCore.GetRuntimeAvailableProviders())
            {
                //Debug.Log($"Evaluating provider {provider.DisplayName}");
                List<AWorkflow> workflows = provider.GetWorkflows();
                // If now workflows, early out.
                if (workflows.Count == 0)
                {
                    continue;
                }

                // Quick optimization: Skip providers that do not support data elements of the type specified by this field.
                if (workflows[0].GetElementTypes().Find(x => m_dataElementType.IsAssignableFrom(x.ElementType)) == null)
                {
                    continue;
                }
                
                foreach (AWorkflow workflow in provider.GetWorkflows())
                {
                    Stack<TreeViewItemData<IDataElement>> traversal = new();
                    Stack<TreeViewItemData<IDataElement>> createOrder = new();

                    foreach (TreeViewItemData<IDataElement> rootElement in workflow.GetTreeViewRootItems())
                    {
                        traversal.Push(rootElement);
                    }

                    while (traversal.Count > 0)
                    {
                        TreeViewItemData<IDataElement> item = traversal.Pop();
                        createOrder.Push(item);
                        foreach (TreeViewItemData<IDataElement> childItem in item.children)
                        {
                            traversal.Push(childItem);
                        }
                    }

                    List<TreeViewItemData<ITreeViewNode>> childrenList = new();
                    Dictionary<TreeViewItemData<IDataElement>, TreeViewItemData<ITreeViewNode>> treeViewItemLookup = new();
                    while (createOrder.Count > 0)
                    {
                        TreeViewItemData<IDataElement> element = createOrder.Pop();
                        foreach (TreeViewItemData<IDataElement> childItem in element.children)
                        {
                            if (treeViewItemLookup.TryGetValue(childItem, out TreeViewItemData<ITreeViewNode> value))
                            {
                                childrenList.Add(value);
                            }
                        }

                        IDataElement dataElement = element.data;
                        Type elementType = dataElement is ILookupTypeOverride typeOverride ? typeOverride.LookupType : dataElement.GetType();
                        
                        bool elementIsSelectable = m_dataElementType.IsAssignableFrom(elementType) 
                                                   && DatastoresEditorUtils.DataElementPassesSearchString(dataElement, searchString);
                        
                        if (elementIsSelectable || childrenList.Count != 0)
                        {
                            //Debug.Log($"Selectable element found: {element.data.DisplayName}");
                            TreeViewItemData<ITreeViewNode> treeViewItemData = new TreeViewItemData<ITreeViewNode>(
                                idCounter++,
                                new DataElementTreeViewNode(element.data, workflow, elementIsSelectable),
                                new List<TreeViewItemData<ITreeViewNode>>(new List<TreeViewItemData<ITreeViewNode>>(childrenList)));
                            
                            treeViewItemLookup.Add(element, treeViewItemData);
                        }
                        
                        childrenList.Clear();
                    }

                    foreach(TreeViewItemData<IDataElement> rootElement in workflow.GetTreeViewRootItems())
                    {
                        if (treeViewItemLookup.TryGetValue(rootElement, out TreeViewItemData<ITreeViewNode> value))
                        {
                            dataElementsTreeViewItems.Add(value);
                        }
                    }
                    treeViewItemLookup.Clear();

                    if (!provider.IsSoloWorkflow)
                    {
                        if (dataElementsTreeViewItems.Count > 0)
                        {
                            //Debug.Log($"Provider is not solo workflow and elements found. Adding workflow {workflow.DisplayName}");
                            workflowTreeViewItems.Add(new TreeViewItemData<ITreeViewNode>(idCounter++,
                                new WorkflowTreeViewNode(workflow, isSearching), new List<TreeViewItemData<ITreeViewNode>>(dataElementsTreeViewItems)));
                        }

                        dataElementsTreeViewItems.Clear();
                    }
                }

                if (provider.IsSoloWorkflow && dataElementsTreeViewItems.Count > 0)
                {
                    //Debug.Log($"Provider is solo workflow and elements found. Adding provider {provider.DisplayName}");
                    tree.Add(new TreeViewItemData<ITreeViewNode>(idCounter++, 
                        new ProviderTreeViewNode(provider, isSearching), new List<TreeViewItemData<ITreeViewNode>>(dataElementsTreeViewItems)));
                }
                else if (workflowTreeViewItems.Count > 0)
                {
                    //Debug.Log($"Provider is not solo workflow and workflows with elements found. Adding provider {provider.DisplayName}");
                    tree.Add(new TreeViewItemData<ITreeViewNode>(idCounter++, 
                        new ProviderTreeViewNode(provider, isSearching), new List<TreeViewItemData<ITreeViewNode>>(workflowTreeViewItems)));
                }
                
                dataElementsTreeViewItems.Clear();
                workflowTreeViewItems.Clear();
            }
            
            m_treeView.SetRootItems(tree);
        }

        private class ITreeViewNode {}

        private class ProviderTreeViewNode : ITreeViewNode
        {
            public ProviderTreeViewNode(AWorkflowProvider provider, bool isSearching)
            {
                Provider = provider;
                IsSearching = isSearching;
            }
            public AWorkflowProvider Provider { get; }
            public bool IsSearching { get; }
        }

        private class WorkflowTreeViewNode : ITreeViewNode
        {
            public WorkflowTreeViewNode(AWorkflow workflow, bool isSearching)
            {
                Workflow = workflow;
                IsSearching = isSearching;
            }
            public AWorkflow Workflow { get; }
            public bool IsSearching { get; }
        }

        private class DataElementTreeViewNode : ITreeViewNode
        {
            public DataElementTreeViewNode(IDataElement dataElement, AWorkflow workflow, bool isSelectable)
            {
                DataElement = dataElement;
                Workflow = workflow;
                IsSelectable = isSelectable;
            }
            public IDataElement DataElement { get; }
            public AWorkflow Workflow { get; }
            public bool IsSelectable { get; }
        }

        private class TreeViewElement : VisualElement
        {
            private ITreeViewNode m_node;
            
            public void SetTreeViewNode(ITreeViewNode node)
            {
                m_node = node;
                if (node is DataElementTreeViewNode dataElementNode)
                {
                    Add(new DataElementTreeViewNodeElement(dataElementNode));
                }
                else if (node is WorkflowTreeViewNode workflowTreeNode)
                {
                    Add(new WorkflowTreeViewNodeElement(workflowTreeNode));
                }
                else if (node is ProviderTreeViewNode providerTreeViewNode)
                {
                    Add(new WorkflowTreeViewNodeElement(providerTreeViewNode));
                }
            }

            public void Reset()
            {
                m_node = null;
                Clear();
            }
        }

        private class DataElementTreeViewNodeElement : VisualElement
        {
            private const string VIEW_UXML = "DatastoresDX/DataElementTreeViewNodeElement";
            private const string TREE_NODE_DRAWER_AREA_TAG = "tree-node-drawer-area";
            private const string FADER_TAG = "fader";
            
            private VisualElement m_treeNodeDrawerArea;
            private VisualElement m_fader;

            public DataElementTreeViewNodeElement(DataElementTreeViewNode node)
            {
                var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
                uxmlAsset.CloneTree(this);
                
                m_treeNodeDrawerArea = this.Q<VisualElement>(TREE_NODE_DRAWER_AREA_TAG);
                m_fader = this.Q<VisualElement>(FADER_TAG);
                
                Type treeNodeDrawerType = DatastoresEditorCore.GetElementTreeNodeDrawerType(node.DataElement);
                ElementTreeNodeDrawer treeNodeDrawer = Activator.CreateInstance(treeNodeDrawerType) as ElementTreeNodeDrawer;
                treeNodeDrawer.style.minHeight = 19;
                treeNodeDrawer.SetElement(new WorkflowElementKey(node.Workflow.Id, node.DataElement.Id));
                m_treeNodeDrawerArea.Add(treeNodeDrawer);
                
                m_fader.style.display = node.IsSelectable ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private class WorkflowTreeViewNodeElement : VisualElement
        {
            private const string VIEW_UXML = "DatastoresDX/WorkflowTreeViewNodeElement";
            private const string WORKFLOW_NAME_LABEL_TAG = "workflow-name-label";
            private const string FADER_TAG = "fader";
            
            private Label m_workflowNameLabel;
            private VisualElement m_fader;
            
            public WorkflowTreeViewNodeElement(WorkflowTreeViewNode node)
            {
                CreateLayout();
                m_workflowNameLabel.text = node.Workflow.DisplayName + " (Workflow)";
                m_fader.style.display = node.IsSearching ? DisplayStyle.Flex : DisplayStyle.None;
            }

            public WorkflowTreeViewNodeElement(ProviderTreeViewNode node)
            {
                CreateLayout();
                if (node.Provider.IsSoloWorkflow)
                {
                    m_workflowNameLabel.text = node.Provider.DisplayName + " (SoloWorkflow)";
                }
                else
                {
                    m_workflowNameLabel.text = node.Provider.DisplayName + " (Provider)";
                }
                m_fader.style.display = DisplayStyle.Flex;
            }

            private void CreateLayout()
            {
                var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
                uxmlAsset.CloneTree(this);
                
                m_workflowNameLabel = this.Q<Label>(WORKFLOW_NAME_LABEL_TAG);
                m_fader = this.Q<VisualElement>(FADER_TAG);
            }
        }
    }
}