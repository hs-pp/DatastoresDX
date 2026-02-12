using System;
using System.Collections.Generic;
using System.Reflection;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;
using UnityEngine;

namespace DatastoresDX.Editor.DataCollections
{
    public class DataCollectionWorkflow : AWorkflow, ILookupTypeOverride
    {
        public override bool CanMoveElements => true;
        public Type LookupType => DataCollection.GetType();
        
        private SerializedProperty m_elementsListSP;
        private SerializedProperty m_treeViewMetasSP;
        private List<DataCollectionElementWrapper> m_elementsBySerializedIndex = new();
        private Dictionary<Uid, DataCollectionElementWrapper> m_idToElement = new();

        public DataCollection DataCollection { get; }
        public SerializedObject DataCollectionSO { get; }
        
        public DataCollectionWorkflow(DataCollection dataCollection)
        {
            DataCollection = dataCollection;
            DataCollectionSO = new SerializedObject(DataCollection);
            Id = dataCollection.Id;
            DisplayName = dataCollection.name;
        }
        
        public override void HandleInitialize()
        {
            m_elementsListSP = DataCollectionSO.FindProperty(DataCollection.Elements_VarName);
            m_treeViewMetasSP = DataCollectionSO.FindProperty(DataCollection.TreeViewMetas_VarName);

            LoadElementWrappers();
        }

        private void LoadElementWrappers()
        {
            m_elementsBySerializedIndex.Clear();
            m_idToElement.Clear();
            
            for (int i = 0; i < m_elementsListSP.arraySize; i++)
            {
                DataCollectionElementWrapper newWrapper = new DataCollectionElementWrapper(DataCollectionSO, i);
                m_elementsBySerializedIndex.Add(newWrapper);
                m_idToElement.Add(newWrapper.Id, newWrapper);
            }
        }

        public override IDataElement GetElementById(Uid id)
        {
            if (id.IsInvalid())
            {
                return null;
            }
            
            if (m_idToElement.TryGetValue(id, out DataCollectionElementWrapper value))
            {
                return value;
            }
            return null;
        }

        protected override List<TreeViewNode> GetTreeViewNodes()
        {
            List<TreeViewNode> treeViewNodes = new();
            foreach(DataCollectionElementWrapper element in m_elementsBySerializedIndex)
            {
                treeViewNodes.Add(new TreeViewNode()
                {
                    Element = element,
                    ParentId = element.ParentId,
                    ChildIds = element.GetChildIds()
                });
            }

            return treeViewNodes;
        }

        protected override Uid HandleElementAdd(Type typeToCreate, Uid clickTargetId)
        {
            if (typeToCreate == null)
            {
                Debug.LogError("[DataAssetWorkflow] Error: Element type to create was null!");
                return Uid.Invalid;
            }
            
            DataCollectionElement newElement = Activator.CreateInstance(typeToCreate) as DataCollectionElement;
            if (newElement == null)
            {
                Debug.LogError("[DataAssetWorkflow] Error: Failed to create new element!");
                return Uid.Invalid;
            }
            var idField = typeof(DataCollectionElement).GetField("m_id", BindingFlags.NonPublic | BindingFlags.Instance);
            idField.SetValue(newElement, DatastoresEditorCore.CrateUniqueId());
            
            m_elementsListSP.InsertArrayElementAtIndex(m_elementsListSP.arraySize);
            m_elementsListSP.GetArrayElementAtIndex(m_elementsListSP.arraySize - 1).managedReferenceValue = newElement;
            
            m_treeViewMetasSP.InsertArrayElementAtIndex(m_treeViewMetasSP.arraySize);
            SerializedProperty newMetaListSp = m_treeViewMetasSP.GetArrayElementAtIndex(m_treeViewMetasSP.arraySize - 1);
            Uid.ToSerializedProperty(newMetaListSp.FindPropertyRelative(TreeViewMeta.ParentId_VarName), clickTargetId);
            newMetaListSp.FindPropertyRelative(TreeViewMeta.ChildsIds_VarName).arraySize = 0;   
            
            DataCollectionElementWrapper newWrapper = new DataCollectionElementWrapper(DataCollectionSO, m_treeViewMetasSP.arraySize - 1);
            m_elementsBySerializedIndex.Add(newWrapper);
            m_idToElement.Add(newWrapper.Id, newWrapper);
            
            DataCollectionElementWrapper parentElement = m_idToElement[clickTargetId];
            parentElement.InsertChildId(newWrapper.Id, -1);

            DataCollectionSO.ApplyModifiedProperties();
            return newWrapper.Id;
        }

        protected override void HandleElementDelete(Uid elementIdToDelete)
        {
            DataCollectionElementWrapperReverseIndexComparer comparer = new DataCollectionElementWrapperReverseIndexComparer();
            DataCollectionElementWrapper toDelete = m_idToElement[elementIdToDelete];
            
            // 1. Get all children and sort by reverse index.
            Stack<DataCollectionElementWrapper> toTraverse = new();
            List<DataCollectionElementWrapper> allDeleteElements = new();
            toTraverse.Push(toDelete);
            while (toTraverse.Count != 0)
            {
                DataCollectionElementWrapper target = toTraverse.Pop();
                allDeleteElements.Add(target);
                List<Uid> childIds = target.GetChildIds();
                for (int i = 0; i < childIds.Count; i++)
                {
                    toTraverse.Push(m_idToElement[childIds[i]]);
                }
            }

            allDeleteElements.Sort(comparer);
            
            // 2. unparent original delete target
            DataCollectionElementWrapper parent = m_idToElement[toDelete.ParentId];
            parent.RemoveChildId(toDelete.Id);
            
            // 3. delete them
            foreach (DataCollectionElementWrapper element in allDeleteElements)
            {
                m_elementsBySerializedIndex.Remove(element);
                m_idToElement.Remove(element.Id);
                DatastoresEditorCore.DestroyUid(element.Id);
                
                m_elementsListSP.DeleteArrayElementAtIndex(element.Index);
                m_treeViewMetasSP.DeleteArrayElementAtIndex(element.Index);
            }
            
            // 4. update existing elements' indices
            for(int i = 0; i < m_elementsBySerializedIndex.Count; i++)
            {
                m_elementsBySerializedIndex[i].SetIndex(i);
            }
            
            DataCollectionSO.ApplyModifiedProperties();
        }
        
        protected override void HandleElementMoved(Uid moveId, Uid newParentId, int childIndex)
        {
            DataCollectionElementWrapper moveElement = m_idToElement[moveId];
            DataCollectionElementWrapper newParentElement = m_idToElement[newParentId];
            DataCollectionElementWrapper oldParentElement = m_idToElement[moveElement.ParentId];
            
            // Validation 
            int counter = 0;
            DataCollectionElementWrapper parentToTraverse = newParentElement;
            while (parentToTraverse != null && counter < 100)
            {
                if (parentToTraverse.Id.Equals(moveId))
                {
                    Debug.LogError("Illegal move.");
                    return;
                }
                parentToTraverse = parentToTraverse.ParentId.IsInvalid() ? null : m_idToElement[parentToTraverse.ParentId];
                counter++;
            }

            if (counter == 100)
            {
                Debug.LogError("[DataCollectionWorkflow] HandleElementMoved: Infinite loop detected. Canceling move.");
                return;
            }

            oldParentElement.RemoveChildId(moveId);
            moveElement.ParentId = newParentId;
            newParentElement.InsertChildId(moveId, childIndex);

            DataCollectionSO.ApplyModifiedProperties();
        }
        
        public override List<ElementTypeDefinition> GetElementTypes()
        {
            return DatastoresEditorCore.GetElementTypes(DataCollection.GetType());
        }

        public override void OnPingRequested()
        {
            EditorGUIUtility.PingObject(DataCollection);
        }

        private class DataCollectionElementWrapperReverseIndexComparer : IComparer<DataCollectionElementWrapper>
        {
            public int Compare(DataCollectionElementWrapper x, DataCollectionElementWrapper y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return y.Index.CompareTo(x.Index);
            }
        }
    }
}