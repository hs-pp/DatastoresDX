using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DatastoresDX.Runtime.DataCollections
{
    /// <summary>
    /// The templated DataCollection PipelineProvider class. For all types that inherit from DataCollection, Datastores
    /// will create and register a new instance of this provider class.
    /// </summary>
    public class DataCollectionPipelineProvider : ATemplatePipelineProvider<DataCollection>
    {
        private bool m_shouldCreateProvider = false;

        public override void OnSetElementType(Type type)
        {
            DataCollectionAttribute attribute =
                type.GetCustomAttribute<DataCollectionAttribute>();
            if (attribute == null)
            {
                Debug.LogWarning($"[{type.Name}] does not have the DataCollectionAttribute!");
                return;
            }
            
            m_shouldCreateProvider = attribute.RuntimeSupported;
        }
        
        protected override async Task<List<APipeline>> LoadPipelines()
        {
            List<APipeline> pipelines = new();
            
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets($"t:{m_elementType.Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                DataCollection dataCollection = AssetDatabase.LoadAssetAtPath<DataCollection>(path);
                pipelines.Add(new DataCollectionPipeline(dataCollection));
            }

            await Task.Delay(1);
#else
            AsyncOperationHandle<IList<IResourceLocation>> locationHandle = Addressables.LoadResourceLocationsAsync(new List<string>(){m_elementType.Name},
                Addressables.MergeMode.Union, typeof(DataCollection));
            await locationHandle.Task;

            AsyncOperationHandle<IList<DataCollection>> dataHandle = Addressables.LoadAssetsAsync<DataCollection>(locationHandle.Result, null);
            await dataHandle.Task;

            foreach (DataCollection dataCollection in dataHandle.Result)
            {
                pipelines.Add(new DataCollectionPipeline(dataCollection));
            }
#endif

            return pipelines;
        }

        public override bool ShouldCreateProvider => m_shouldCreateProvider;
    }
}