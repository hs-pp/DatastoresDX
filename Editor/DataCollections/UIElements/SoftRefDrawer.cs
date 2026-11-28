using System;
using System.Reflection;
using System.Text.RegularExpressions;
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
    [CustomPropertyDrawer(typeof(SceneRef))]
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

    /// <summary>
    /// Sent up the tree when a SoftRef field attaches, so the inspector panel can restore and track its preview.
    /// </summary>
    public class SoftRefAttachedEvent : EventBase<SoftRefAttachedEvent>
    {
        public SoftRefElement Field { get; private set; }

        public static SoftRefAttachedEvent GetPooled(SoftRefElement field)
        {
            SoftRefAttachedEvent evt = GetPooled();
            evt.target = field;
            evt.Field = field;
            return evt;
        }

        public SoftRefAttachedEvent()
        {
            LocalInit();
        }

        protected override void Init()
        {
            base.Init();
            LocalInit();
        }

        private void LocalInit()
        {
            bubbles = true;
            tricklesDown = false;
            Field = null;
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
        private VisualElement m_previewContent;
        private Func<Object, VisualElement> m_previewContentFactory;

        private SerializedProperty m_assetGuidSP;
        private SerializedProperty m_guidSP;
        private FieldInfo m_fieldInfo;
        private Type m_assetType;
        private Object m_object;
        private bool m_previewOpen;

        // Path inside the element, like m_graph.
        public string RelativePropertyPath { get; }
        public event Action<bool> PreviewOpenChanged;

        public bool PreviewOpen
        {
            get => m_previewOpen;
            set
            {
                if (value && m_object != null)
                {
                    ShowPreview();
                }
                else
                {
                    HidePreview();
                }
            }
        }

        // Builds what the preview shows for the referenced object. Null uses the asset's default inspector.
        public Func<Object, VisualElement> PreviewContentFactory
        {
            get => m_previewContentFactory;
            set
            {
                m_previewContentFactory = value;
                if (m_previewOpen)
                {
                    ShowPreview();
                }
            }
        }

        public SoftRefElement(SerializedProperty property, FieldInfo fieldInfo)
        {
            m_assetGuidSP = property;
            m_guidSP = property.FindPropertyRelative(BaseSoftRef.Guid_VarName);
            m_fieldInfo = fieldInfo;
            m_assetType = m_fieldInfo.FieldType == typeof(SceneRef) ? typeof(SceneAsset) : m_fieldInfo.FieldType.GetGenericArguments()[0];
            RelativePropertyPath = Regex.Replace(property.propertyPath, @"^m_elements\.Array\.data\[\d+\]\.", "");

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
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // The PropertyField parent only exists once attached.
            SetFillsSpace(m_previewOpen);
            using (SoftRefAttachedEvent attachedEvent = SoftRefAttachedEvent.GetPooled(this))
            {
                SendEvent(attachedEvent);
            }
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
                // Scenes have nothing to preview.
                m_togglePreviewButton.style.display = m_assetType == typeof(SceneAsset) ? DisplayStyle.None : DisplayStyle.Flex;
                m_createNewButton.style.display = DisplayStyle.None;
                // Keeps an open preview open on the new object.
                PreviewOpen = m_previewOpen;
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
            SetPreviewOpenFlag(false);
            m_previewContainer.style.display = DisplayStyle.None;
            SetFillsSpace(false);
            ClearPreviewContent();
        }

        private void ShowPreview()
        {
            SetPreviewOpenFlag(true);
            m_previewContainer.style.display = DisplayStyle.Flex;
            SetFillsSpace(true);
            ClearPreviewContent();
            m_previewContent = m_previewContentFactory != null ? m_previewContentFactory(m_object) : CreateDefaultPreviewContent(m_object);
            m_previewContainer.Add(m_previewContent);
        }

        // Removing the content detaches it, so content holding resources should release them on DetachFromPanelEvent.
        private void ClearPreviewContent()
        {
            if (m_previewContent == null)
            {
                return;
            }

            if (m_previewContent is InspectorElement inspectorElement)
            {
                inspectorElement.Unbind();
            }
            m_previewContent.RemoveFromHierarchy();
            m_previewContent = null;
        }

        private static VisualElement CreateDefaultPreviewContent(Object obj)
        {
            InspectorElement inspectorElement = new InspectorElement();
            inspectorElement.Bind(new SerializedObject(obj));
            return inspectorElement;
        }

        private void SetPreviewOpenFlag(bool open)
        {
            if (m_previewOpen == open)
            {
                return;
            }
            m_previewOpen = open;
            PreviewOpenChanged?.Invoke(open);
        }

        // An open preview stretches to fill the inspector.
        private void SetFillsSpace(bool fill)
        {
            StyleFloat grow = fill ? 1 : StyleKeyword.Null;
            style.flexGrow = grow;
            PropertyField propertyField = GetFirstAncestorOfType<PropertyField>();
            if (propertyField != null)
            {
                propertyField.style.flexGrow = grow;
            }
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
            PreviewOpen = !m_previewOpen;
        }
    }
}