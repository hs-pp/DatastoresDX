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
                GameObject gameObject = obj as GameObject;
                return gameObject != null ? gameObject.GetComponent<T>() : null;
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

            string address = GetAddressablePath(m_guid);
            if (!typeof(Component).IsAssignableFrom(typeof(T)))
            {
                return Addressables.LoadAssetAsync<T>(address);
            }

            // Addressables stores prefabs as GameObjects, so asking for a component type directly finds nothing
            // (InvalidKeyException). Load the GameObject and take the component from it.
            // releaseDependenciesOnFailure: false means the GameObject load is released exactly once, when the returned
            // handle is released, even if the load fails.
            AsyncOperationHandle<GameObject> gameObjectHandle = Addressables.LoadAssetAsync<GameObject>(address);
            AsyncOperationHandle<T> componentHandle = Addressables.ResourceManager.CreateChainOperation<T, GameObject>(
                gameObjectHandle, GetComponentFromGameObject, releaseDependenciesOnFailure: false);
            gameObjectHandle.Release(); // The chain holds its own reference to the GameObject load.
            return componentHandle;
#endif
        }

        // Outside the #if so editor compiles check it too.
        private static AsyncOperationHandle<T> GetComponentFromGameObject(AsyncOperationHandle<GameObject> gameObjectHandle)
        {
            if (gameObjectHandle.Status != AsyncOperationStatus.Succeeded || gameObjectHandle.Result == null)
            {
                return Addressables.ResourceManager.CreateCompletedOperation<T>(null,
                    $"SoftRef<{typeof(T).Name}>: failed to load the GameObject ({gameObjectHandle.DebugName}).");
            }

            T component = gameObjectHandle.Result.GetComponent<T>();
            if (component == null)
            {
                return Addressables.ResourceManager.CreateCompletedOperation<T>(null,
                    $"SoftRef<{typeof(T).Name}>: {gameObjectHandle.Result.name} has no {typeof(T).Name} component.");
            }

            return Addressables.ResourceManager.CreateCompletedOperation(component, string.Empty);
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
