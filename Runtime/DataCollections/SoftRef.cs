using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DatastoresDX.Runtime.DataCollections
{
    public abstract class BaseSoftRef
    {
        [SerializeField]
        protected string m_guid;
        public string Guid => m_guid;
        
        public bool IsNull() => string.IsNullOrEmpty(m_guid);
        
        public static string GetAddressablePath(string guid)
        {
            /// This must match DataCollectionAnalyzeRule.GetBundledAssetAddresssableName()!!!
            return $"BundledAsset/{guid}";
        }
        
#if UNITY_EDITOR
        public BundleAssetConfig ToBundleAssetConfig()
        {
            return new BundleAssetConfig()
            {
                AssetGuid = m_guid,
            };
        }
        
        public static string Guid_VarName = "m_guid";
#endif
    }
    
    /// <summary>
    /// This thing is cool. MAKE IT A STRUCT??
    /// </summary>
    [Serializable]
    public class SoftRef<T> : BaseSoftRef where T : UnityEngine.Object
    {
        public T Get()
        {
            if (string.IsNullOrEmpty(m_guid))
            {
                return null;
            }
            
#if UNITY_EDITOR
            string path = AssetDatabase.GUIDToAssetPath(m_guid);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
            AsyncOperationHandle<UnityEngine.Object> handle = Addressables.LoadAssetAsync<UnityEngine.Object>(GetAddressablePath(m_guid));
            UnityEngine.Object obj = handle.WaitForCompletion();
            if(typeof(Component).IsAssignableFrom(typeof(T)))
            {
                return (obj as GameObject).GetComponent<T>();
            }
            else
            {
                return obj as T;
            }
#endif
        }

        public AsyncOperationHandle<T> GetAsync()
        {
#if UNITY_EDITOR
            // In editor we fake it.
            return Addressables.ResourceManager.CreateCompletedOperation(Get(), null);
#else
            if (string.IsNullOrEmpty(m_guid))
            {
                // GOTCHA: AsyncOperationHandle.Task from a default returns a Task that never finishes!! Do we need a better solution?
                return default;
            }
            return Addressables.LoadAssetAsync<T>(GetAddressablePath(m_guid));
#endif
        }
        
        // DelayedOperation if we ever want to mock slow load times when we call GetAsync().
        // public class DelayedOperation<T> : AsyncOperationBase<T>
        // {
        //     private float m_delaySeconds;
        //     private T m_result;
        //
        //     public void Init(T result, float delay)
        //     {
        //         m_delaySeconds = delay;
        //         m_result = result;
        //     }
        //
        //     protected override async void Execute()
        //     {
        //         await UniTask.WaitForSeconds(m_delaySeconds);
        //         Complete(m_result, true, null);
        //     }
        // }
    }
}
