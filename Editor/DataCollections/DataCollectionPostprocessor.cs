using System;
using System.Threading.Tasks;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;

namespace DatastoresDX.Editor.DataCollections
{
    public class DataCollectionPostprocessor : UnityEditor.AssetModificationProcessor
    {
        static void OnWillCreateAsset(string assetName)
        {
            ProcessModifiedAsset(assetName, 100, 0);
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetName, RemoveAssetOptions options)
        {
            ProcessModifiedAsset(assetName, 0, 100);
            return AssetDeleteResult.DidNotDelete;
        }

        static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            ProcessModifiedAsset(destinationPath, 100, 0);
            return AssetMoveResult.DidNotMove;
        }

        static async void DelayedProcessModifiedAsset(string path)
        {
            await Task.Delay(100); // Hack to wait for operation to be finished.
            ProcessModifiedAsset(path, 100, 0);
        }
        
        private static async void ProcessModifiedAsset(string path, int delay1, int delay2)
        {
            if (delay1 != 0)
            {
                await Task.Delay(delay1);
            }
            
            DataCollection dataCollection = AssetDatabase.LoadAssetAtPath<DataCollection>(path);
            if (dataCollection == null)
            {
                return;
            }
            
            Type dataCollectionType = dataCollection.GetType();
            if(delay2 != 0)
            {
                await Task.Delay(delay2);
            }
            
            AWorkflowProvider provider =
                DatastoresEditorCore.GetTemplateWorkflowProviderByTemplateWorkflowItemType(dataCollectionType);
            provider.Initialize();
            DatastoresEditorCore.NotifyWorkflowProviderUpdated(provider.Id);
        }
    }
}