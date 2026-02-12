using System;
using System.Reflection;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Label = UnityEngine.UIElements.Label;
using Object = UnityEngine.Object;

namespace DatastoresDX.Editor.DataCollections
{
    [CustomPropertyDrawer(typeof(SoftRef<>))]
    public class SoftRefDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (PropertyPathIsValid(property) && typeof(DataCollection).IsAssignableFrom(property.serializedObject.targetObject.GetType()))
            {
                return new SoftRefElement(property, fieldInfo);
            }
            else
            {
                return new Label("[SoftRef must be a property of DataCollectionElement!]");
            }
        }

        // Property path must match the pattern:
        // m_elements.Array.data[0].varname
        // or
        // m_elements.Array.data[0].varname.Array.data[0].varname
        // where 0 is any number and varname is any var name.
        private bool PropertyPathIsValid(SerializedProperty property)
        {
            string pattern = @"^m_elements\.Array\.data\[\d+\]\.(\w+)(?:\.Array\.data\[\d+\]\.(\w+))?$";
            return System.Text.RegularExpressions.Regex.IsMatch(property.propertyPath, pattern);
        }
    }

    public class SoftRefElement : VisualElement
    {
        private const string VIEW_UXML = "DatastoresDX/DataCollections/SoftRefDrawer";
        private const string OBJECT_FIELD_TAG = "object-field";
        private const string CREATE_NEW_BUTTON_TAG = "create-new-button";
        private const string TOGGLE_PREVIEW_BUTTON_TAG = "toggle-preview-button";
        private const string PREVIEW_CONTAINER_TAG = "preview-container";
        
        private ObjectField m_objectField;
        private Button m_createNewButton;
        private Button m_togglePreviewButton;
        private VisualElement m_previewContainer;
        private InspectorElement m_inspectorElement;
        
        private SerializedProperty m_assetGuidSP;
        private SerializedProperty m_guidSP;
        private FieldInfo m_fieldInfo;
        private Type m_assetType;
        private Object m_object;

        public SoftRefElement(SerializedProperty property, FieldInfo fieldInfo)
        {
            m_assetGuidSP = property;
            m_guidSP = property.FindPropertyRelative(BaseSoftRef.Guid_VarName);
            m_fieldInfo = fieldInfo;
            m_assetType = m_fieldInfo.FieldType.GetGenericArguments()[0];
            
            CreateLayout();
            SetupView();
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);
            
            m_objectField = this.Q<ObjectField>(OBJECT_FIELD_TAG);
            m_createNewButton = this.Q<Button>(CREATE_NEW_BUTTON_TAG);
            m_togglePreviewButton = this.Q<Button>(TOGGLE_PREVIEW_BUTTON_TAG);
            m_previewContainer = this.Q<VisualElement>(PREVIEW_CONTAINER_TAG);
            m_inspectorElement = new InspectorElement();
            
            m_objectField.allowSceneObjects = false;
            m_objectField.label = m_assetGuidSP.displayName;
            m_objectField.objectType = m_assetType;
            m_objectField.RegisterValueChangedCallback(OnObjectFieldChanged);
            
            m_createNewButton.clicked += () =>
            {
                ScriptableObject newSo = CreateNewAsset();
                m_guidSP.stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(newSo));
                m_assetGuidSP.serializedObject.ApplyModifiedProperties();
                SetupView();
            };
            
            m_togglePreviewButton.clicked += OnPreviewToggleClicked;
        }

        private void SetupView()
        {
            string guid = m_guidSP.stringValue;
            if (!string.IsNullOrEmpty(guid))
            {
                m_object = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                if (m_object == null)
                {
                    Debug.LogError($"[SoftRef] Could not find Asset!!");
                }
            }
            else
            {
                m_object = null;
            }
            
            
            if (m_object is DataCollection)
            {
                Debug.LogError($"[SoftRef] Object is of type DataCollection which is not allowed. Resetting guid to none.");
                m_guidSP.stringValue = "";
                m_guidSP.serializedObject.ApplyModifiedProperties();
                SetupView();
                return;
            }
            
            m_objectField.SetValueWithoutNotify(m_object);
            if (m_object != null)
            {
                m_objectField.SetValueWithoutNotify(m_object);
                m_togglePreviewButton.style.display = DisplayStyle.Flex;
                m_createNewButton.style.display = DisplayStyle.None;
                HidePreview();
            }
            else
            {
                m_togglePreviewButton.style.display = DisplayStyle.None;
                if (typeof(ScriptableObject).IsAssignableFrom(m_assetType))
                {
                    //m_createNewButton.style.display = DisplayStyle.Flex;
                }
                m_createNewButton.style.display = DisplayStyle.None; // Let's just keep this button always off for now since it's sorta ugly.
                HidePreview();
            }
        }

        private void HidePreview()
        {
            m_previewContainer.style.display = DisplayStyle.None;
            m_inspectorElement.Unbind();
            if (m_inspectorElement.parent == m_previewContainer)
            {
                m_previewContainer.Remove(m_inspectorElement);
            }
        }

        private void ShowPreview()
        {
            m_previewContainer.style.display = DisplayStyle.Flex;
            m_inspectorElement.Bind(new SerializedObject(m_object));
            m_previewContainer.Add(m_inspectorElement);
        }

        private void OnObjectFieldChanged(ChangeEvent<Object> evt)
        {
            if (evt.newValue != null)
            {
                m_guidSP.stringValue = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(evt.newValue));
            }
            else
            {
                m_guidSP.stringValue = string.Empty;
            }
            m_assetGuidSP.serializedObject.ApplyModifiedProperties();

            SetupView();
        }
        
        private ScriptableObject CreateNewAsset()
        {
            string filePath = EditorUtility.SaveFilePanelInProject(
                $"New {m_assetType.Name} Location", $"New{m_assetType.Name}", "asset", "");
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }
            
            ScriptableObject so = ScriptableObject.CreateInstance(m_assetType);
            AssetDatabase.CreateAsset(so, filePath);
            AssetDatabase.SaveAssets();

            return so;
        }

        private void OnPreviewToggleClicked()
        {
            if (m_previewContainer.style.display == DisplayStyle.None)
            {
                ShowPreview();
            }
            else
            {
                HidePreview();
            }
        }
    }
}