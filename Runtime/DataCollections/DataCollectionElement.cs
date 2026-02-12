using System;
using System.Collections.Generic;
using UnityEngine;

namespace DatastoresDX.Runtime.DataCollections
{
    /// <summary>
    /// Base class for DataElements managed by DataCollections.
    /// </summary>
    [Serializable]
    public abstract class DataCollectionElement : IDataElement
    {
        [SerializeField]
        protected Uid m_id = Uid.Invalid;
        public Uid Id => m_id;
        [SerializeField]
        protected string m_displayName;
        public string DisplayName => m_displayName;

#if UNITY_EDITOR

        public virtual List<BundleAssetConfig> GetAssetsToBundle()
        {
            return new();
        }
        
        public static string Id_VarName = "m_id";
        public static string DisplayName_VarName = "m_displayName";
#endif
    }
    
    public class RootDataCollectionElement : DataCollectionElement
    {
        public RootDataCollectionElement()
        {
            m_id = Uid.Invalid;
            m_displayName = "Root";
        }
    }

#if UNITY_EDITOR
    public struct BundleAssetConfig
    {
        public string AssetGuid;
        public List<string> Labels;
    }
#endif
}