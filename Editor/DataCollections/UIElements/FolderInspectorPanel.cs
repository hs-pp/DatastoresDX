using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor.DataCollections
{
    [InspectorPanel(typeof(FolderElement))]
    public class FolderInspectorPanel : AInspectorPanel
    {
        private const string VIEW_UXML = "DatastoresDX/DataCollections/FolderInspectorPanel";
        private const string ID_PROPERTYFIELD_TAG = "id-propertyfield";
        private const string SWAP_VIEW_BUTTON_TAG = "swap-view-button";
        private const string VIEW_MODE_TAG = "view-mode";
        private const string FOLDER_NAME_LABEL_TAG = "folder-name-label";
        private const string DESC_LABEL_TAG = "desc-label";
        private const string EDIT_MODE_TAG = "edit-mode";
        private const string FOLDER_NAME_TEXTFIELD_TAG = "folder-name-textfield";
        private const string DESC_TEXTFIELD_TAG = "desc-textfield";
        
        private PropertyField m_idPropertyField;
        private Button m_swapViewButton;
        private VisualElement m_viewMode;
        private Label m_folderNameLabel;
        private Label m_descLabel;
        private VisualElement m_editMode;
        private TextField m_folderNameTextField;
        private TextField m_descTextField;

        private bool m_showingEditView;

        protected override VisualElement CreatePanel()
        {
            VisualElement root = new();
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(root);

            m_idPropertyField = root.Q<PropertyField>(ID_PROPERTYFIELD_TAG);
            m_swapViewButton = root.Q<Button>(SWAP_VIEW_BUTTON_TAG);
            m_viewMode = root.Q<VisualElement>(VIEW_MODE_TAG);
            m_folderNameLabel = root.Q<Label>(FOLDER_NAME_LABEL_TAG);
            m_descLabel = root.Q<Label>(DESC_LABEL_TAG);
            m_editMode = root.Q<VisualElement>(EDIT_MODE_TAG);
            m_folderNameTextField = root.Q<TextField>(FOLDER_NAME_TEXTFIELD_TAG);
            m_descTextField = root.Q<TextField>(DESC_TEXTFIELD_TAG);

            m_swapViewButton.clickable.clicked += ToggleView;

            ShowView(false);
            return root;
        }

        protected override void OnSetElement(WorkflowElementKey elementKey)
        {
            DataCollectionElementWrapper element = elementKey.GetElement() as DataCollectionElementWrapper;
            if (element == null)
            {
                return;
            }

            SerializedProperty elementSP = element.ElementSP;

            m_idPropertyField.BindProperty(elementSP.FindPropertyRelative(DataCollectionElement.Id_VarName));
            m_folderNameLabel.BindProperty(elementSP.FindPropertyRelative(DataCollectionElement.DisplayName_VarName));
            m_descLabel.BindProperty(elementSP.FindPropertyRelative(FolderElement.Desc_VarName));
            m_folderNameTextField.BindProperty(elementSP.FindPropertyRelative(DataCollectionElement.DisplayName_VarName));
            m_descTextField.BindProperty(elementSP.FindPropertyRelative(FolderElement.Desc_VarName));
        }
        
        private void ShowView(bool showEditView)
        {
            m_showingEditView = showEditView;
            m_viewMode.style.display = m_showingEditView ? DisplayStyle.None : DisplayStyle.Flex;
            m_editMode.style.display = m_showingEditView ? DisplayStyle.Flex : DisplayStyle.None;
        }
        private void ToggleView()
        {
            ShowView(!m_showingEditView);
        }
    }
}