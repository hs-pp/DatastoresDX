using UnityEngine;

namespace DatastoresDX.Runtime.DataCollections
{
    /// <summary>
    /// Simple folder element available to all DataCollections.
    /// </summary>
    [DataElement(typeof(DataCollection), "Folder", "DatastoresDX/DataCollections/FolderIcon")]
    public class FolderElement : DataCollectionElement
    {
        [SerializeField]
        private string m_desc;

        public FolderElement()
        {
            m_displayName = "Folder";
        }

#if UNITY_EDITOR
        public static string Desc_VarName = "m_desc";
#endif
    }
}