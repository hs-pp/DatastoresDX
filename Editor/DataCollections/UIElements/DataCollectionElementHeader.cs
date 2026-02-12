using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor.DataCollections
{
    public class DataCollectionElementHeader : VisualElement
    {
        private const string VIEW_UXML = "DatastoresDX/DataCollections/DataCollectionElementHeader";
        private const string UID_FIELD_TAG = "uid-field";
        private const string SCRIPT_LABEL_TAG = "script-label";
        private const string NAME_LABEL_TAG = "name-label";
        private const string NAME_TEXTFIELD_TAG = "name-textfield";

        private UidElement m_uidElement;
        private Label m_scriptLabel;
        private Label m_nameLabel;
        private TextField m_nameTextField;
        
        public DataCollectionElementHeader()
        {
            CreateLayout();
        }

        public void Bind(SerializedProperty dataCollectionElementSP)
        {
            Unbind();
            if (dataCollectionElementSP == null)
            {
                return;
            }

            m_uidElement.SetUuid(
                Uid.FromSerializedProperty(
                    dataCollectionElementSP.FindPropertyRelative(DataCollectionElement.Id_VarName)));
            m_scriptLabel.text = dataCollectionElementSP.managedReferenceValue.GetType().Name;
            SerializedProperty nameSP =
                dataCollectionElementSP.FindPropertyRelative(DataCollectionElement.DisplayName_VarName);
            m_nameLabel.BindProperty(nameSP);
            m_nameTextField.BindProperty(nameSP);
        }

        public void Unbind()
        {

        }

        private void ShowNameTextField(bool showTextField)
        {
            m_nameLabel.style.display = showTextField ? DisplayStyle.None : DisplayStyle.Flex;
            m_nameTextField.style.display = showTextField ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);

            m_uidElement = this.Q<UidElement>(UID_FIELD_TAG);
            m_scriptLabel = this.Q<Label>(SCRIPT_LABEL_TAG);
            m_nameLabel = this.Q<Label>(NAME_LABEL_TAG);
            m_nameTextField = this.Q<TextField>(NAME_TEXTFIELD_TAG);

            m_nameLabel.style.display = DisplayStyle.Flex;
            m_nameTextField.style.display = DisplayStyle.None;
            
            Clickable nameDoubleClick = new Clickable(OnNameDoubleClicked);
            nameDoubleClick.activators.Clear();
            nameDoubleClick.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            m_nameLabel.AddManipulator(nameDoubleClick);

            m_nameTextField.RegisterCallback<FocusOutEvent>(OnNameTextFieldFocusLost);
            
            Clickable scriptDoubleClick = new Clickable(OnScriptLabelDoubleClicked);
            scriptDoubleClick.activators.Clear();
            scriptDoubleClick.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            m_scriptLabel.AddManipulator(scriptDoubleClick);
        }

        private void OnNameDoubleClicked()
        {
            ShowNameTextField(true);
            m_nameTextField.Focus();
        }
        
        private void OnNameTextFieldFocusLost(FocusOutEvent evt)
        {
            ShowNameTextField(false);
        }
        
        private void OnScriptLabelDoubleClicked()
        {
            string strToCheck = $"/{m_scriptLabel.text}.cs";
            string[] guids = AssetDatabase.FindAssets($"t:script {m_scriptLabel.text}");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (assetPath.Contains(strToCheck))
                {
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath));
                    return;
                }
            }
            Debug.LogError($"Could not find {m_scriptLabel.text}.cs.");
        }
    }
}