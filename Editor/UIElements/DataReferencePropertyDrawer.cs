using System;
using DatastoresDX.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace DatastoresDX.Editor
{
    [CustomPropertyDrawer(typeof(DataReference<>))]
    public class DataReferencePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var dataType = fieldInfo.FieldType.GetGenericArguments()[0];
            // Dumbass unity doing dumbass things. If this drawer is for a collection, the fieldInfo is wrong.
            if (dataType.IsGenericType && typeof(DataReference<>) == dataType.GetGenericTypeDefinition())
            {
                dataType = dataType.GetGenericArguments()[0];
            }
            return new DataReferenceField(property, new DataReferenceDrawer(property, dataType));
        }
    }
    
    public class DataReferenceField : BaseField<DataReference<IDataElement>>
    {
        private DataReferenceDrawer m_dataReferenceDrawer;
        public DataReferenceField(SerializedProperty dataReferenceSP, DataReferenceDrawer visualInput) : base(dataReferenceSP.displayName, visualInput)
        {
            AddToClassList(alignedFieldUssClassName);
            m_dataReferenceDrawer = visualInput;
            
            this.TrackPropertyValue(dataReferenceSP, property =>
            {
                m_dataReferenceDrawer.RefreshElement();
            });
        }
    }

    [UxmlElement]
    public partial class DataReferenceDrawer : VisualElement
    {
        private static string VIEW_UXML = "DatastoresDX/DataReferenceDrawer";
        private const string UID_ELEMENT_TAG = "uid-element";
        private const string NO_ELEMENT_SELECTED_LABEL_TAG = "no-element-selected-label";
        private const string INVALID_ELEMENT_ERROR_LABEL_TAG = "invalid-element-error-label";
        private const string ELEMENT_TREE_NODE_AREA_TAG = "element-tree-node-area";

        private UidElement m_uidElement;
        private Label m_noElementSelectedLabel;
        private Label m_invalidElementErrorLabel;
        private VisualElement m_elementTreeNodeArea;

        private SerializedProperty m_dataReferenceSP;
        private Type m_dataElementType;
        private Uid m_dataElementId = Uid.Invalid;

        public Action<Uid> OnValueChanged;
        
        public DataReferenceDrawer(SerializedProperty dataReferenceSP, Type dataElementType)
        {
            m_dataReferenceSP = dataReferenceSP;
            SetElementType(dataElementType);
            
            CreateLayout();
            LoadProperty();
            
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnMouseUp);
        }

        // For non-serialized DataReferences
        public DataReferenceDrawer()
        {
            CreateLayout();
            LoadProperty();
            
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnMouseUp);
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);

            m_uidElement = this.Q<UidElement>(UID_ELEMENT_TAG);
            m_noElementSelectedLabel = this.Q<Label>(NO_ELEMENT_SELECTED_LABEL_TAG);
            m_invalidElementErrorLabel = this.Q<Label>(INVALID_ELEMENT_ERROR_LABEL_TAG);
            m_elementTreeNodeArea = this.Q<VisualElement>(ELEMENT_TREE_NODE_AREA_TAG);
            
            this.AddManipulator(new Clickable(() =>
            {
                PopupWindow.Show(worldBound, new DataReferenceSelectorPopup(m_dataElementType,
                    id => { TrySetDataElement(id, true); }));
            }));
        }

        private void LoadProperty()
        {
            if (m_dataReferenceSP != null)
            {
                m_dataElementId = Uid.FromSerializedProperty(
                    m_dataReferenceSP.FindPropertyRelative(DataReference<IDataElement>.DataElementId_VarName));                
            }

            if (m_dataElementId.IsInvalid())
            {
                ShowNoElementSelected();
                return;
            }

            WorkflowElementKey elementKey = DatastoresEditorCore.GetDataElementKey(m_dataElementId, true);
            if (elementKey.GetElement() == null)
            {
                ShowInvalidElementError(m_dataElementId);
                return;
            }
            
            ShowElement(elementKey);
        }

        public void SetElementType(Type dataElementType)
        {
            m_dataElementType = dataElementType;
        }

        public void SetDataElementId(Uid dataElementId, bool notify = true)
        {
            TrySetDataElement(dataElementId, notify);
        }

        public void RefreshElement()
        {
            LoadProperty();
        }

        private void ShowNoElementSelected()
        {
            m_uidElement.SetUuid(Uid.Invalid);
            m_noElementSelectedLabel.style.display = DisplayStyle.Flex;
            m_invalidElementErrorLabel.style.display = DisplayStyle.None;
            m_elementTreeNodeArea.style.display = DisplayStyle.None;
        }

        private void ShowInvalidElementError(Uid invalidId)
        {
            m_uidElement.SetUuid(Uid.Invalid);
            m_noElementSelectedLabel.style.display = DisplayStyle.None;
            m_invalidElementErrorLabel.style.display = DisplayStyle.Flex;
            m_elementTreeNodeArea.style.display = DisplayStyle.None;
        }

        private void ShowElement(WorkflowElementKey elementKey)
        {
            IDataElement element = elementKey.GetElement();
            
            m_uidElement.SetUuid(element.Id);
            m_noElementSelectedLabel.style.display = DisplayStyle.None;
            m_invalidElementErrorLabel.style.display = DisplayStyle.None;
            m_elementTreeNodeArea.style.display = DisplayStyle.Flex;
            
            m_elementTreeNodeArea.Clear();
            
            Type treeNodeDrawerType = DatastoresEditorCore.GetElementTreeNodeDrawerType(element);
            ElementTreeNodeDrawer treeNodeDrawer = Activator.CreateInstance(treeNodeDrawerType) as ElementTreeNodeDrawer;
            treeNodeDrawer.style.minHeight = 19;
            treeNodeDrawer.SetElement(elementKey);
            m_elementTreeNodeArea.Add(treeNodeDrawer);
        }

        private void OnDragUpdated(DragUpdatedEvent e)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
        
        private void OnMouseUp(DragPerformEvent e)
        {
            object data = DragAndDrop.GetGenericData("__unity-drag-and-drop__source-view");
            if (data is TreeView treeView)
            {
                IDataElement draggedDataElement = treeView.selectedItem as IDataElement;
                if (draggedDataElement == null)
                {
                    return;
                }
                
                TrySetDataElement(draggedDataElement.Id);
                e.StopImmediatePropagation();
            }
        }
        
        private void TrySetDataElement(Uid elementId, bool notify = true)
        {
            // If element is not invalid, check if it's kosher.
            if (!elementId.IsInvalid())
            {
                IDataElement element = DatastoresEditorCore.GetDataElementKey(elementId, true).GetElement();
                if (element == null)
                {
                    Debug.LogError("[DataReference] Could not find element. Is it possibly an editor element?");
                    return;
                }

                Type elementType = element.GetType();
                if (element is ILookupTypeOverride typeOverride)
                {
                    elementType = typeOverride.LookupType;
                }

                if (m_dataElementType != null && !m_dataElementType.IsAssignableFrom(elementType))
                {
                    Debug.LogError(
                        $"[DataReference] Incorrect element type. {m_dataElementType.Name} and {elementType.Name}");
                    return;
                }
            }

            // Set the element.
            m_dataElementId = elementId;
            
            if (m_dataReferenceSP != null)
            {
                SerializedProperty idSP =
                    m_dataReferenceSP.FindPropertyRelative(DataReference<IDataElement>.DataElementId_VarName);
                Uid.ToSerializedProperty(idSP, elementId);
                idSP.serializedObject.ApplyModifiedProperties();
            }
            else
            {
                RefreshElement();
            }

            if (notify)
            {
                OnValueChanged?.Invoke(m_dataElementId);
            }
        }
        
        public void ToggleUidDisplay(bool show)
        {
            m_uidElement.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}