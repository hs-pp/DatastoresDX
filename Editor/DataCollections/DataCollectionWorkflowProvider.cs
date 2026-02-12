using System;
using System.Collections.Generic;
using System.Reflection;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;
using UnityEngine;

namespace DatastoresDX.Editor.DataCollections
{
    public class DataCollectionWorkflowProvider : ATemplateWorkflowProvider<DataCollection>
    {
        private bool m_isSoloWorkflow;
        public override bool IsSoloWorkflow => m_isSoloWorkflow;

        public override void OnSetElementType(Type type)
        {
            DataCollectionAttribute attribute =
                type.GetCustomAttribute<DataCollectionAttribute>();
            if (attribute != null)
            {
                m_isSoloWorkflow = attribute.IsSoloWorkflow;
                
                if (attribute.DisplayNameOverride != null)
                {
                    m_displayName = attribute.DisplayNameOverride;
                }
            }
        }

        protected override List<AWorkflow> LoadWorkflows()
        {
            List<AWorkflow> workflows = new();
            string[] guids = AssetDatabase.FindAssets("t:"+ m_elementType.FullName);
            for(int i = 0; i<guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                DataCollectionWorkflow workflow =  new DataCollectionWorkflow(AssetDatabase.LoadAssetAtPath(path, m_elementType) as DataCollection);
                workflows.Add(workflow);
            }

            return workflows;
        }
        
        protected override AWorkflow HandleCreateNewWorkflow()
        {
            string filePath = EditorUtility.SaveFilePanelInProject(
                $"New {DisplayName} Location", $"New{DisplayName}", "asset", 
                "Ya dog.");
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }
            
            DataCollection so = (DataCollection)ScriptableObject.CreateInstance(m_elementType);
            var idField = typeof(DataCollection).GetField("m_id", BindingFlags.NonPublic | BindingFlags.Instance);
            idField.SetValue(so, DatastoresEditorCore.CrateUniqueId());
            
            AssetDatabase.CreateAsset(so, filePath);
            AssetDatabase.SaveAssets();
            
            DataCollectionWorkflow workflow = new DataCollectionWorkflow(so);
            return workflow;
        }

        protected override bool HandleDeleteWorkflow(Uid workflowId)
        {
            DataCollectionWorkflow workflow = GetWorkflow(workflowId) as DataCollectionWorkflow;
            if(EditorUtility.DisplayDialog("Delete Workflow?", $"Are you sure you want to delete {workflow.DisplayName}?", "Yes", "No"))
            {
                ScriptableObject dataAsset = workflow.DataCollection;
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(dataAsset));
                DatastoresEditorCore.DestroyUid(workflowId);

                return true;   
            }

            return false;
        }
    }
}