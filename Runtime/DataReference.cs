using System;
using UnityEngine;

namespace DatastoresDX.Runtime
{
    /// <summary>
    /// A serialized reference to a DataElement.
    /// This should be the main way to store references to data elements in Unity land.
    /// </summary>
    [Serializable]
    public struct DataReference<T> where T : IDataElement
    {
        [SerializeField]
        private Uid m_dataElementId;
        public Uid DataElementId => m_dataElementId;
        public bool IsNull => m_dataElementId.IsInvalid();
        
        public DataReference(Uid uid)
        {
            m_dataElementId = uid;
        }

        public DataReference(string uidString)
        {
            m_dataElementId = Uid.FromString(uidString);
        }
        
        public T Get()
        {
            return (T)Datastores.GetElement(m_dataElementId);
        }

        public override bool Equals(object obj)
        {
            if (obj is DataReference<T> other)
            {
                return m_dataElementId.Equals(other.m_dataElementId);
            }

            return false;
        }
        
        public override int GetHashCode()
        {
            return m_dataElementId.GetHashCode();
        }

#if UNITY_EDITOR
        
        public void SetReference(Uid dataElementId)
        {
            // Probably should type check against T, but we only use this in editor aka through the DataReferenceDrawer
            // which means it already gets type checked.
            m_dataElementId = dataElementId;
        }
        
        public static string DataElementId_VarName = "m_dataElementId";
#endif
    }
}
