using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.ResourceManagement.ResourceLocations;
#endif

namespace DatastoresDX.Runtime.DataCollections
{
    /// <summary>
    /// Soft reference to a scene.
    /// </summary>
    [Serializable]
    public class SceneRef : BaseSoftRef
    {
#if UNITY_EDITOR
        // SceneProvider is internal to Addressables.
        private const string SCENE_PROVIDER_ID = "UnityEngine.ResourceManagement.ResourceProviders.SceneProvider";
#endif

        /// <summary>
        /// Always returns a valid handle. Release it if it failed, otherwise unload it with Addressables.UnloadSceneAsync.
        /// </summary>
        public AsyncOperationHandle<SceneInstance> LoadAsync(LoadSceneMode loadMode)
        {
            if (IsNull())
            {
                return Addressables.ResourceManager.CreateCompletedOperation<SceneInstance>(default, "SceneRef: no scene is set.");
            }

#if UNITY_EDITOR
            // In editor load from the AssetDatabase so the scene doesn't need an Addressables entry.
            // Same location Addressables builds for scenes in its AssetDatabase play mode.
            string path = AssetDatabase.GUIDToAssetPath(m_guid);
            if (string.IsNullOrEmpty(path))
            {
                return Addressables.ResourceManager.CreateCompletedOperation<SceneInstance>(default, $"SceneRef: no scene with guid {m_guid}.");
            }

            IResourceLocation location = new ResourceLocationBase(GetAddressablePath(m_guid), path, SCENE_PROVIDER_ID, typeof(SceneInstance));
            return Addressables.LoadSceneAsync(location, loadMode);
#else
            return Addressables.LoadSceneAsync(GetAddressablePath(m_guid), loadMode);
#endif
        }
    }
}
