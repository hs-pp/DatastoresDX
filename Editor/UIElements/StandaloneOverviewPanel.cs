using System;
using DatastoresDX.Editor.DataCollections;
using DatastoresDX.Runtime;
using UnityEditor;
using UnityEngine;

namespace DatastoresDX.Editor
{
    public class StandaloneOverviewPanel : EditorWindow
    {
        [SerializeReference]
        private AOverviewPanel m_overviewPanel;

        public void SetWorkflow(Uid workflowId)
        {
            if (workflowId.IsInvalid())
            {
                return;
            }

            AWorkflow workflow = DatastoresEditorCore.GetWorkflow(workflowId);
            Type overviewPanelType = DatastoresEditorCore.GetOverviewPanelType(workflow);
            if (overviewPanelType == null)
            {
                overviewPanelType = typeof(DefaultOverviewPanel);
            }
            m_overviewPanel = Activator.CreateInstance(overviewPanelType) as AOverviewPanel;
            if(m_overviewPanel == null)
            {
                Debug.LogError($"[StandaloneOverviewPanel] Failed to create overview panel for workflow: {workflow.DisplayName}");
                return;
            }
            
            m_overviewPanel.SetWorkflow(workflowId);
            OnEnable();
        }

        private void OnEnable()
        {
            rootVisualElement.Clear();
            if (m_overviewPanel == null)
            {
                return;
            }
            rootVisualElement.Add(m_overviewPanel.GetPanel());
        }

        public void OnDisable()
        {
            m_overviewPanel.SaveState();
        }
    }
}