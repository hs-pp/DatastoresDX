using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    [Serializable]
    public abstract class AInspectorPanel
    {
        protected VisualElement m_panel;
        [SerializeField]
        protected WorkflowElementKey m_elementkey;
        
        public VisualElement GetPanel()
        {
            if (m_panel == null)
            {
                m_panel = CreatePanel();
                m_panel.style.flexGrow = 1;
                LoadElement();
            }
            return m_panel;
        }

        public void SetElement(WorkflowElementKey elementKey)
        {
            m_elementkey = elementKey;
            GetPanel();
            OnSetElement(elementKey);
        }

        public void LoadElement()
        {
            if (!m_elementkey.IsInvalid())
            {
                SetElement(m_elementkey);
            }
        }
        
        protected abstract VisualElement CreatePanel();
        protected abstract void OnSetElement(WorkflowElementKey elementKey);
        public virtual void SaveState() { }
    }
}