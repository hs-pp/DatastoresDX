using System;
using DatastoresDX.Runtime;
using UnityEngine;

namespace DatastoresDX.Editor
{
    [Serializable]
    public class DatastoresWindowState
    {
        [SerializeField]
        public float SplitViewPosition = 200;
        [SerializeField]
        public bool IsShowingEditorTab = true;

        [SerializeField]
        public Uid SelectedWorkflowId = Uid.Invalid; // If this id is invalid, we are in WorkflowListPanel.
        [SerializeField]
        public string SearchFieldValue;
        [SerializeField]
        public Uid SelectedElementId = Uid.Invalid;
        [SerializeField]
        public DetailsPanel.SelectedTab SelectedTab;
        [SerializeReference]
        public AInspectorPanel InspectorPanel;
        [SerializeReference]
        public AOverviewPanel OverviewPanel;

        public void ClearWorkflowSettings()
        {
            SelectedWorkflowId = Uid.Invalid;
            SearchFieldValue = "";
            SelectedElementId = Uid.Invalid;
            SelectedTab = DetailsPanel.SelectedTab.OVERVIEW;
            InspectorPanel = null;
            OverviewPanel = null;
        }
    }
}