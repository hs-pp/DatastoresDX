using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DatastoresDX.Runtime.DataCollections
{
    [Serializable]
    public abstract class DataCollection : ScriptableObject, IDataElement
    {
        [SerializeField]
        protected Uid m_id = Uid.Invalid;
        public Uid Id => m_id;
        public string DisplayName => name;
        
        [SerializeReference]
        protected List<DataCollectionElement> m_elements = new();
        [SerializeField]
        protected List<TreeViewMeta> m_treeViewMetas = new();

        public DataCollection()
        {
            m_elements.Add(new RootDataCollectionElement());
            m_treeViewMetas.Add(new TreeViewMeta(){ ParentId = Uid.Invalid });
        }

        public List<DataCollectionElement> GetAllElements()
        {
            return m_elements.GetRange(1, m_elements.Count - 1);
        }
        
        public DataCollectionElement GetElement(Uid id)
        {
            return m_elements.Find(element => element.Id.Equals(id));
        }
        
#if UNITY_EDITOR
        public static string Id_VarName = "m_id";
        public static string Elements_VarName = "m_elements";
        public static string TreeViewMetas_VarName = "m_treeViewMetas";

        public static List<SerializedProperty> GetFullPath(SerializedProperty elementSP)
        {
            SerializedObject serializedObject = elementSP.serializedObject;
            SerializedProperty allElementsSP = serializedObject.FindProperty(Elements_VarName);
            for (int i = 0; i < allElementsSP.arraySize; i++)
            {
                if (allElementsSP.GetArrayElementAtIndex(i).propertyPath == elementSP.propertyPath)
                {
                    return GetFullPath(allElementsSP, i);
                }
            }

            return new List<SerializedProperty>() { elementSP };
        }

        private static List<SerializedProperty> GetFullPath(SerializedProperty allElementsSP, int arrayIndex)
        {
            SerializedProperty elementSP = allElementsSP.GetArrayElementAtIndex(arrayIndex);
            SerializedProperty parentIdSP = allElementsSP.serializedObject
                .FindProperty(TreeViewMetas_VarName).GetArrayElementAtIndex(arrayIndex).FindPropertyRelative(TreeViewMeta.ParentId_VarName);

            // If parent id is invalid we out. We directly check the SP to not have to allocate a new Uid.
            if (parentIdSP.FindPropertyRelative(Uid.Value_VarName).intValue == 0)
            {
                return new List<SerializedProperty>() { elementSP };
            }

            for (int i = 0; i < allElementsSP.arraySize; i++)
            {
                if (UidSPsAreEqual(parentIdSP,
                        allElementsSP.GetArrayElementAtIndex(i).FindPropertyRelative(DataCollectionElement.Id_VarName)))
                {
                    List<SerializedProperty> parentFullPath = GetFullPath(allElementsSP, i);
                    parentFullPath.Add(elementSP);
                    return parentFullPath;
                }
            }

            return new List<SerializedProperty>() { elementSP };
        }

        private static bool UidSPsAreEqual(SerializedProperty one, SerializedProperty two)
        {
            return one.FindPropertyRelative(Uid.Value_VarName).intValue == two.FindPropertyRelative(Uid.Value_VarName).intValue;
        }
#endif
    }
    
    [Serializable]
    public struct TreeViewMeta
    {
        public Uid ParentId;
        public List<Uid> ChildIds;

#if UNITY_EDITOR
        public static string ParentId_VarName = "ParentId";
        public static string ChildsIds_VarName = "ChildIds";
#endif
    }
}