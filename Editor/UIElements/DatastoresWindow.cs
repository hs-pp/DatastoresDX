using DatastoresDX.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public class DatastoresWindow : EditorWindow, IHasCustomMenu
    {
        private const string VIEW_UXML = "DatastoresDX/DatastoresWindow";
        private const string SPLITVIEW_TAG = "split-view";
        private const string SPLITVIEW_CONTENT_CONTAINER_TAG = "unity-content-container";
        private const string WORKFLOW_LIST_PANEL_TAG = "workflow-list-panel";
        private const string ELEMENT_LIST_PANEL_TAG = "element-list-panel";
        private const string WORKFLOW_TABS_VIEW_TAG = "details-panel";
        
        private TwoPaneSplitView m_splitView;
        private WorkflowListPanel m_workflowListPanel;
        private ElementListPanel m_elementListPanel;
        private DetailsPanel m_detailsPanel;

        [SerializeField]
        private DatastoresWindowState m_state = new();

        #region Init
        [MenuItem("HS/DatastoresDX")]
        public static void OpenWindow()
        {
            CreateWindow<DatastoresWindow>().Show();
        }

        public void OnEnable()
        {
            titleContent = new GUIContent("DDX");
            
            CreateLayout();
            SetupCallbacks();

            LoadState();
        }

        public void OnDisable()
        {
            TeardownCallbacks();
            m_detailsPanel?.SaveState();
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(rootVisualElement);

            m_splitView = rootVisualElement.Q<TwoPaneSplitView>(SPLITVIEW_TAG);
            m_workflowListPanel = rootVisualElement.Q<WorkflowListPanel>(WORKFLOW_LIST_PANEL_TAG);
            m_workflowListPanel.SetWorkflowWindow(this, m_state);
            m_elementListPanel = rootVisualElement.Q<ElementListPanel>(ELEMENT_LIST_PANEL_TAG);
            m_detailsPanel = rootVisualElement.Q<DetailsPanel>(WORKFLOW_TABS_VIEW_TAG);
            m_elementListPanel.SetWorkflowWindow(this, m_detailsPanel);
        }

        private void SetupCallbacks()
        {
            VisualElement splitViewFixedPanel =
                m_splitView.Q<VisualElement>(SPLITVIEW_CONTENT_CONTAINER_TAG)[m_splitView.fixedPaneIndex];
            splitViewFixedPanel.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                m_state.SplitViewPosition = evt.newRect.width;
            });
            Undo.undoRedoPerformed += HandleOnUndo;
            
            DatastoresEditorCore.OnReloaded += OnCoreReloaded;
            DatastoresEditorCore.OnWorkflowProviderUpdated += OnCoreWorkflowProviderUpdated;
            DatastoresEditorCore.OnWorkflowUpdated += OnCoreWorkflowUpdated;
        }

        private void TeardownCallbacks()
        {
            Undo.undoRedoPerformed -= HandleOnUndo;

            DatastoresEditorCore.OnReloaded -= OnCoreReloaded;
            DatastoresEditorCore.OnWorkflowProviderUpdated -= OnCoreWorkflowProviderUpdated;
            DatastoresEditorCore.OnWorkflowUpdated -= OnCoreWorkflowUpdated;
        }

        #endregion
        
        private void LoadState()
        {
            if (m_splitView.fixedPaneInitialDimension.Equals(m_state.SplitViewPosition))
            {
                // If we set the initial dimension to the same value, it doesnt actually refresh the splitview.
                // so we gotta set it to something different, and then re-set it to get it to guarantee refresh.
                // It's really stupid the splitview doesnt have a built in "refresh" function.
                m_splitView.fixedPaneInitialDimension = m_state.SplitViewPosition + 1;
            }

            m_splitView.fixedPaneInitialDimension = m_state.SplitViewPosition;

            m_elementListPanel.OpenWorkflow(m_state);
            m_workflowListPanel.ToggleView(m_state.SelectedWorkflowId.IsInvalid());
        }

        public void ReloadState()
        {
            LoadState();
        }
        
        /// <summary>
        /// Passing in an Invalid id defaults the window to PipelineList mode.
        /// </summary>
        public void OpenWorkflow(Uid workflowId)
        {
            if (DatastoresEditorCore.GetWorkflow(workflowId) == null)
            {
                workflowId = Uid.Invalid;
            }
            
            m_state.ClearWorkflowSettings();
            m_state.SelectedWorkflowId = workflowId;
            m_elementListPanel.OpenWorkflow(m_state);
            m_workflowListPanel.ToggleView(workflowId.IsInvalid());
        }
        
        private void OnCoreReloaded()
        {
            ReloadState();
        }

        private void OnCoreWorkflowProviderUpdated(Uid providerId)
        {
            m_workflowListPanel.Reload();
            ReloadState();
        }
        
        private void OnCoreWorkflowUpdated(Uid workflowId)
        {
            if (m_state.SelectedWorkflowId.Equals(workflowId))
            {
                ReloadState();
            }
        }
        
        private void HandleOnUndo()
        {
            DatastoresEditorCore.Reload();
        }
        
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Reload Everything"), false, () =>
            {
                DatastoresEditorCore.Reload();
            });
            menu.AddItem(new GUIContent("Log EditorCore"), false, () =>
            {
                DatastoresEditorCore.PrintAllData();
            });
            menu.AddItem(new GUIContent("Log State"), false, () => 
            {
                Debug.Log(JsonUtility.ToJson(m_state, true));
            });
        }
    }
}