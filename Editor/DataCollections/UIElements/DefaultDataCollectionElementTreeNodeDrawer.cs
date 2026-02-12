using System;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor.DataCollections
{
    [ElementTreeNodeDrawer(typeof(DataCollectionElement))]
    public class DefaultDataCollectionElementTreeNodeDrawer : ElementTreeNodeDrawer
    {
        private const string VIEW_UXML = "DatastoresDX/DefaultElementTreeNodeDrawer";
        private const string ICON_TAG = "icon";
        private const string LABEL_TAG = "label";
        private const string TYPE_LABEL_TAG = "type-label";

        private VisualElement m_icon;
        private Label m_label;
        private Label m_typeLabel;

        public DefaultDataCollectionElementTreeNodeDrawer()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);

            m_icon = this.Q<VisualElement>(ICON_TAG);
            m_label = this.Q<Label>(LABEL_TAG);
            m_typeLabel = this.Q<Label>(TYPE_LABEL_TAG);
        }

        public override void SetElement(WorkflowElementKey elementKey)
        {
            DataCollectionElementWrapper wrapper = elementKey.GetElement() as DataCollectionElementWrapper;
            m_label.BindProperty(wrapper.ElementSP.FindPropertyRelative(DataCollectionElement.DisplayName_VarName));

            m_typeLabel.text = wrapper.LookupType.Name;
            m_typeLabel.style.display = wrapper.RuntimeElement is FolderElement ? DisplayStyle.None : DisplayStyle.Flex;
            
            // Icon
            string iconPath = DatastoresEditorCore.GetIconPath(wrapper.LookupType);
            Texture2D iconTexture = Resources.Load<Texture2D>(iconPath);
            m_icon.style.display = iconTexture == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (iconTexture != null)
            {
                m_icon.style.backgroundImage = iconTexture;
            }
            else
            {
                Debug.LogError($"Icon not found for type {wrapper.LookupType.Name}");
            }
        }
        
        public override void ResetElement()
        {
            m_label.Unbind();
            m_icon.style.display = DisplayStyle.None;
            m_icon.style.backgroundImage = null;
        }
        
    }
}