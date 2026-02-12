using System;
using System.Collections.Generic;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;

namespace DatastoresDX.Editor.DataCollections
{
    public class DataCollectionElementWrapper : IDataElement, ILookupTypeOverride
    {
        public Uid Id { get; private set; }
        public string DisplayName { get; private set; }
        public Type LookupType => ElementSP.managedReferenceValue.GetType();
        public IDataElement RuntimeElement => ElementSP.managedReferenceValue as IDataElement;

        private SerializedObject DataCollectionSO { get; }
        public SerializedProperty ElementSP { get; private set; }
        
        private int m_index = -1;
        private SerializedProperty m_treeViewMetaSP;
        private Uid m_parentId = Uid.Invalid;
        private List<Uid> m_childIds = new();
        
        public DataCollectionElementWrapper(SerializedObject dataCollectionSO, int elementIndex)
        {
            DataCollectionSO = dataCollectionSO;
            SetIndex(elementIndex);
        }

        public void SetIndex(int index)
        {
            m_index = index;
            ElementSP = DataCollectionSO.FindProperty(DataCollection.Elements_VarName).GetArrayElementAtIndex(m_index);
            m_treeViewMetaSP = DataCollectionSO.FindProperty(DataCollection.TreeViewMetas_VarName).GetArrayElementAtIndex(m_index);
            Id = Uid.FromSerializedProperty(ElementSP.FindPropertyRelative(DataCollectionElement.Id_VarName));
            DisplayName = ElementSP.FindPropertyRelative(DataCollectionElement.DisplayName_VarName).stringValue;
            
            m_parentId = Uid.FromSerializedProperty(m_treeViewMetaSP.FindPropertyRelative(TreeViewMeta.ParentId_VarName));
            m_childIds.Clear();
            SerializedProperty childIdsSP = m_treeViewMetaSP.FindPropertyRelative(TreeViewMeta.ChildsIds_VarName);
            for (int i = 0; i < childIdsSP.arraySize; i++)
            {
                m_childIds.Add(Uid.FromSerializedProperty(childIdsSP.GetArrayElementAtIndex(i)));
            }
        }

        public int Index => m_index;
        public Uid ParentId
        {
            get => m_parentId;
            set
            {
                Uid.ToSerializedProperty(m_treeViewMetaSP.FindPropertyRelative(TreeViewMeta.ParentId_VarName), value);
                m_parentId = value;
            }
        }
        
        public List<Uid> GetChildIds()
        {
            return m_childIds;
        }
        
        public void InsertChildId(Uid childId, int index = -1)
        {
            SerializedProperty childIdsSP = m_treeViewMetaSP.FindPropertyRelative(TreeViewMeta.ChildsIds_VarName);
            if (index == -1)
            {
                index = childIdsSP.arraySize;
            }
            m_childIds.Insert(index, childId);
            childIdsSP.InsertArrayElementAtIndex(index);
            Uid.ToSerializedProperty(childIdsSP.GetArrayElementAtIndex(index), childId);
        }
        
        public void RemoveChildId(Uid childId)
        {
            int index = m_childIds.IndexOf(childId);
            if (index != -1)
            {
                m_childIds.RemoveAt(index);
                m_treeViewMetaSP.FindPropertyRelative(TreeViewMeta.ChildsIds_VarName).DeleteArrayElementAtIndex(index);
            }
        }
    }
}