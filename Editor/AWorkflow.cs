using System;
using System.Collections.Generic;
using DatastoresDX.Runtime;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public class WorkflowComparer : IComparer<AWorkflow>
    {
        public int Compare(AWorkflow x, AWorkflow y)
        {
            return x.DisplayName.CompareTo(y.DisplayName);
        }
    }
    
    /// <summary>
    /// The concept of a Workflow only exists at editor time.
    /// </summary>
    public abstract class AWorkflow
    {
        public Uid Id { get; protected set; }
        public string DisplayName { get; protected set; }
        public abstract bool CanMoveElements { get; }

        private bool m_isInitialized;
        private List<TreeViewItemData<IDataElement>> m_cachedTreeViewRootItems = new();
        private Dictionary<Uid, int> m_elementIdToTreeViewId = new();
        
        public void Initialize()
        {
            if (!m_isInitialized)
            {
                HandleInitialize();
                m_isInitialized = true;
            }
        }
        
        public List<IDataElement> GetAllElements()
        {
            List<TreeViewItemData<IDataElement>> rootElements = GetTreeViewRootItems();
            List<IDataElement> allElements = new List<IDataElement>();
            Stack<TreeViewItemData<IDataElement>> traversal = new Stack<TreeViewItemData<IDataElement>>(rootElements);
            while (traversal.Count != 0)
            {
                TreeViewItemData<IDataElement> element = traversal.Pop();
                allElements.Add(element.data);
                foreach (TreeViewItemData<IDataElement> child in element.children)
                {
                    traversal.Push(child);
                }
            }

            return allElements;
        }
        
        public List<TreeViewItemData<IDataElement>> GetTreeViewRootItems()
        {
            if(m_cachedTreeViewRootItems.Count == 0)
            {
                LoadTreeViewRootItems(GetTreeViewNodes());
            }
            return new List<TreeViewItemData<IDataElement>>(m_cachedTreeViewRootItems);
        }
        
        private void LoadTreeViewRootItems(List<TreeViewNode> treeViewNodes)
        {
            m_cachedTreeViewRootItems.Clear();
            if (treeViewNodes == null)
            {
                return;
            }
            
            // Set up treeview ids.
            m_elementIdToTreeViewId.Clear();
            int counter = 0;
            foreach (TreeViewNode node in treeViewNodes)
            {
                m_elementIdToTreeViewId.Add(node.Element.Id, counter++);
            }
            
            Dictionary<Uid, TreeViewNode> nodeLookup = new();
            Stack<TreeViewNode> traversal = new Stack<TreeViewNode>();
            Stack<TreeViewNode> createOrder = new Stack<TreeViewNode>();
            
            foreach (TreeViewNode node in treeViewNodes)
            {
                if (node.Element.Id.IsInvalid())
                {
                    traversal.Push(node);
                }
                
                nodeLookup.Add(node.Element.Id, node);
            }
            
            while (traversal.Count != 0)
            {
                TreeViewNode element = traversal.Pop();
                createOrder.Push(element);
                foreach (Uid childId in element.ChildIds)
                {
                    traversal.Push(nodeLookup[childId]);
                }
            }
            
            List<TreeViewItemData<IDataElement>> childrenList = new();
            Dictionary<TreeViewNode, TreeViewItemData<IDataElement>> treeViewItemDatas = new();

            while (createOrder.Count != 0)
            {
                TreeViewNode element = createOrder.Pop();
                foreach (Uid childId in element.ChildIds)
                {
                    childrenList.Add(treeViewItemDatas[nodeLookup[childId]]);
                }

                TreeViewItemData<IDataElement> treeViewItemData =
                    new TreeViewItemData<IDataElement>(m_elementIdToTreeViewId[element.Element.Id],
                        element.Element, new List<TreeViewItemData<IDataElement>>(childrenList));
                treeViewItemDatas.Add(element, treeViewItemData);

                childrenList.Clear();
            }

            // Root nodes skip the true root element.
            TreeViewItemData<IDataElement> root = treeViewItemDatas[nodeLookup[Uid.Invalid]];
            foreach (TreeViewItemData<IDataElement> child in root.children)
            {
                m_cachedTreeViewRootItems.Add(child);
            }
        }
        
        public int GetTreeViewId(Uid elementId)
        {
            return m_elementIdToTreeViewId[elementId];
        }
        
        public Uid AddElement(Type typeToCreate, Uid clickTargetId)
        {
            Uid newId = HandleElementAdd(typeToCreate, clickTargetId);
            LoadTreeViewRootItems(GetTreeViewNodes()); // TODO: Eventually we should just add the new element to the treeview rather than rebuilding the whole thing.
            return newId;
        }
        
        public void DeleteElement(Uid elementIdToDelete)
        {
            HandleElementDelete(elementIdToDelete);
            LoadTreeViewRootItems(GetTreeViewNodes()); // TODO: Eventually we should just remove the element from the treeview rather than rebuilding the whole thing.
        }
        
        public void MoveElement(Uid moveId, Uid newParentId, int childIndex)
        {
            HandleElementMoved(moveId, newParentId, childIndex);
            LoadTreeViewRootItems(GetTreeViewNodes()); // TODO: Eventually we should update rather than rebuilding the whole thing.
        }
        
        public virtual List<ElementTypeDefinition> GetElementTypes()
        {
            return DatastoresEditorCore.GetElementTypes(GetType());
        }
        
        public abstract void HandleInitialize();
        public abstract IDataElement GetElementById(Uid id);
        protected abstract List<TreeViewNode> GetTreeViewNodes();
        protected abstract Uid HandleElementAdd(Type typeToCreate, Uid clickTargetId);
        protected abstract void HandleElementDelete(Uid elementIdToDelete);
        protected abstract void HandleElementMoved(Uid moveId, Uid newParentId, int childIndex);
        public virtual void OnPingRequested() { }
    }
    
    [Serializable]
    public class TreeViewNode
    {
        public IDataElement Element;
        public Uid ParentId; // Invalid = at root level
        public List<Uid> ChildIds;
    }
}