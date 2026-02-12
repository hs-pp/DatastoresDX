using System;
using DatastoresDX.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    [UxmlElement]
    public partial class DetailsPanel : VisualElement
    {
        private const string VIEW_UXML = "DatastoresDX/DetailsPanel";
        private const string SCROLLVIEW_TAG = "scrollview";
        private const string OVERVIEW_TAB_TAG = "overview-tab";
        private const string INSPECTOR_TAB_TAG = "inspector-tab";
        private const string OVERVIEW_LABEL_TAG = "overview-label";
        private const string INSPECTOR_LABEL_TAG = "inspector-label";
        private const string OVERVIEW_PANEL_TAG = "overview-panel";
        private const string INSPECTOR_PANEL_TAG = "inspector-panel";
        private const string FADE_ELEMENT_TAG = "fade-element";
        private static readonly Color SELECTED_TAB_COLOR = new Color(0.2196f, 0.2196f, 0.2196f);
        private static readonly Color UNSELECTED_TAB_COLOR = new Color(0.1647f, 0.1647f, 0.1647f);
        private static readonly Color SELECTED_LABEL_COLOR = new Color(0.5234375f, 0.5234375f, 0.5234375f);
        private static readonly Color UNSELECTED_LABEL_COLOR = new Color(0.34375f, 0.34375f, 0.34375f);
        private static readonly Color UNSELECTED_BOTTOM_BORDER_COLOR = new Color(0.07843f, 0.07843f, 0.07843f);

        private VisualElement m_overviewTab;
        private VisualElement m_inspectorTab;
        private Label m_overviewLabel;
        private Label m_inspectorLabel;
        private VisualElement m_overviewPanelArea;
        private VisualElement m_inspectorPanelArea;
        private VisualElement m_fadeElement;
        
        private DatastoresWindowState m_state;
        private AWorkflow m_workflow;
        
        public Action<SelectedTab> OnSelectedTabChanged;

        public DetailsPanel()
        {
            CreateLayout();
            SetupCallbacks();
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);

            ScrollView scrollView = this.Q<ScrollView>(SCROLLVIEW_TAG);
            VisualElement contentContainer = scrollView.Q<VisualElement>("unity-content-container");
            contentContainer.style.flexGrow = 1;
            
            m_overviewTab = this.Q<VisualElement>(OVERVIEW_TAB_TAG);
            m_inspectorTab = this.Q<VisualElement>(INSPECTOR_TAB_TAG);
            m_overviewLabel = this.Q<Label>(OVERVIEW_LABEL_TAG);
            m_inspectorLabel = this.Q<Label>(INSPECTOR_LABEL_TAG);
            m_overviewPanelArea = this.Q<VisualElement>(OVERVIEW_PANEL_TAG);
            m_inspectorPanelArea = this.Q<VisualElement>(INSPECTOR_PANEL_TAG);
            m_fadeElement = this.Q<VisualElement>(FADE_ELEMENT_TAG);
        }

        private void SetupCallbacks()
        {
            m_overviewTab.RegisterCallback<MouseDownEvent>(evt => { SelectTab(SelectedTab.OVERVIEW); });
            m_inspectorTab.RegisterCallback<MouseDownEvent>(evt => { SelectTab(SelectedTab.INSPECTOR); });

            m_overviewTab.AddManipulator(new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
            {
                evt.menu.AppendAction("Popout", action => PopoutOverviewView());
            }));
            m_inspectorTab.AddManipulator(new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
            {
                evt.menu.AppendAction("Popout", action => PopoutInspectorView());
            }));
        }

        private void ClearPanel()
        {
            m_overviewPanelArea.Clear();
            m_inspectorPanelArea.Clear();
            
            SelectTab(SelectedTab.OVERVIEW);
        }

        public void SaveState()
        {
            if (m_state == null)
            {
                return;
            }
            
            if (m_state.OverviewPanel != null)
            {
                m_state.OverviewPanel.SaveState();
            }
            if (m_state.InspectorPanel != null)
            {
                m_state.InspectorPanel.SaveState();
            }
        }
        
        public void OpenWorkflow(DatastoresWindowState state)
        {
            m_state = state;
            m_workflow = m_state != null ? DatastoresEditorCore.GetWorkflow(m_state.SelectedWorkflowId) : null;
            m_fadeElement.style.display = (m_workflow == null) ? DisplayStyle.Flex : DisplayStyle.None;

            if (m_workflow == null)
            {
                ClearPanel();
                return;
            }

            PopulateOverviewTab();
            SelectTab(m_state.SelectedTab);

            IDataElement selectedElement = m_workflow.GetElementById(m_state.SelectedElementId);
            if (selectedElement == null)
            {
                m_state.SelectedElementId = Uid.Invalid;
            }
            PopulateInspectorTab(selectedElement);
        }

        public void SelectTab(SelectedTab selectedTab)
        {
            //Debug.Log("Selecting tab " + selectedTab + " save to state: " + saveToState);
            m_overviewTab.style.backgroundColor = selectedTab == SelectedTab.OVERVIEW 
                ? SELECTED_TAB_COLOR 
                : UNSELECTED_TAB_COLOR;
            m_inspectorTab.style.backgroundColor = selectedTab == SelectedTab.INSPECTOR 
                ? SELECTED_TAB_COLOR 
                : UNSELECTED_TAB_COLOR;

            m_overviewTab.style.borderBottomColor = selectedTab == SelectedTab.OVERVIEW
                ? SELECTED_TAB_COLOR
                : UNSELECTED_BOTTOM_BORDER_COLOR;
            m_inspectorTab.style.borderBottomColor = selectedTab == SelectedTab.INSPECTOR
                ? SELECTED_TAB_COLOR
                : UNSELECTED_BOTTOM_BORDER_COLOR;
            
            m_overviewPanelArea.style.display = selectedTab == SelectedTab.OVERVIEW ? DisplayStyle.Flex : DisplayStyle.None;
            m_inspectorPanelArea.style.display = selectedTab == SelectedTab.INSPECTOR ? DisplayStyle.Flex : DisplayStyle.None;

            m_overviewLabel.style.color = selectedTab == SelectedTab.OVERVIEW 
                ? SELECTED_LABEL_COLOR 
                : UNSELECTED_LABEL_COLOR;
            m_inspectorLabel.style.color = selectedTab == SelectedTab.INSPECTOR 
                ? SELECTED_LABEL_COLOR 
                : UNSELECTED_LABEL_COLOR;

            if (m_state != null)
            {
                m_state.SelectedTab = selectedTab;
            }

            OnSelectedTabChanged?.Invoke(selectedTab);
        }

        public void SelectElement(IDataElement selectedElement)
        {
            if (selectedElement == null)
            {
                SelectTab(SelectedTab.OVERVIEW);
                return;
            }
            
            PopulateInspectorTab(selectedElement);
            SelectTab(SelectedTab.INSPECTOR);
        }
        
        private void PopulateOverviewTab()
        {
            m_overviewPanelArea.Clear();
            if (m_state == null)
            {
                return;
            }

            Type overviewPanelType = DatastoresEditorCore.GetOverviewPanelType(m_workflow);
            if (!(m_state.OverviewPanel != null && 
                  m_state.OverviewPanel.GetType() == overviewPanelType))
            {
                m_state.OverviewPanel = Activator.CreateInstance(overviewPanelType) as AOverviewPanel;
            }

            if (m_state.OverviewPanel == null)
            { 
                Debug.LogError($"[DetailsPanel] No OverviewPanel found for {m_workflow.GetType()}");
                return;
            }
            
            m_overviewPanelArea.Add(m_state.OverviewPanel.GetPanel());
            m_state.OverviewPanel.SetWorkflow(m_workflow.Id);  
        }

        private void PopulateInspectorTab(IDataElement element)
        {
            m_inspectorPanelArea.Clear();
            if (m_state == null)
            {
                return;
            }

            if (element == null)
            {
                return;
            }

            Type inspectorPanelType = DatastoresEditorCore.GetInspectorPanelType(element);
            if (!(m_state.InspectorPanel != null && m_state.InspectorPanel.GetType() == inspectorPanelType))
            {
                m_state.InspectorPanel = Activator.CreateInstance(inspectorPanelType) as AInspectorPanel;
            }

            if (m_state.InspectorPanel == null)
            {
                Debug.LogError($"[DetailsPanel] No InspectorPanel found for {element.GetType()}");
                return;
            }
            
            m_inspectorPanelArea.Add(m_state.InspectorPanel.GetPanel());
            m_state.InspectorPanel.SetElement(new WorkflowElementKey(m_workflow.Id, element.Id));
        }
        
        private void PopoutOverviewView()
        {
            StandaloneOverviewPanel popout = EditorWindow.CreateWindow<StandaloneOverviewPanel>();
            popout.SetWorkflow(m_state.SelectedWorkflowId);
        }

        private void PopoutInspectorView()
        {
            StandaloneInspectorPanel popout = EditorWindow.CreateWindow<StandaloneInspectorPanel>();
            popout.SetElement(new WorkflowElementKey(m_state.SelectedWorkflowId, m_state.SelectedElementId));
        }
        
        [Serializable]
        public enum SelectedTab
        {
            OVERVIEW = 0,
            INSPECTOR = 1
        }
    }
}