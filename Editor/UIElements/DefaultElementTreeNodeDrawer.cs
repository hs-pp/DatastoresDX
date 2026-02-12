using System;
using DatastoresDX.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public class DefaultElementTreeNodeDrawer : ElementTreeNodeDrawer
    {
        private const string VIEW_UXML = "DatastoresDX/DefaultElementTreeNodeDrawer";
        private const string ICON_TAG = "icon";
        private const string LABEL_TAG = "label";
        private const string TYPE_LABEL_TAG = "type-label";

        private VisualElement m_icon;
        private Label m_label;
        private Label m_typeLabel;

        public DefaultElementTreeNodeDrawer()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);

            m_icon = this.Q<VisualElement>(ICON_TAG);
            m_label = this.Q<Label>(LABEL_TAG);
            m_typeLabel = this.Q<Label>(TYPE_LABEL_TAG);
        }

        public override void SetElement(WorkflowElementKey elementKey)
        {
            IDataElement element = elementKey.GetElement();
            m_label.text = element.DisplayName;

            Type elementType = element.GetType();
            if (element is ILookupTypeOverride typeOverride)
            {
                elementType = typeOverride.LookupType;
            }

            m_typeLabel.text = elementType.Name;
            
            // Icon
            string iconPath = DatastoresEditorCore.GetIconPath(elementType);
            Texture2D iconTexture = Resources.Load<Texture2D>(iconPath);
            m_icon.style.display = iconTexture == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (iconTexture != null)
            {
                m_icon.style.backgroundImage = iconTexture;
            }
            else
            {
                Debug.LogError($"Icon not found for type {elementType.Name}");
            }
        }
    }
}