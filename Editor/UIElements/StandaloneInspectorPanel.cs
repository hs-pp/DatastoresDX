using System;
using DatastoresDX.Runtime;
using UnityEditor;
using UnityEngine;

namespace DatastoresDX.Editor
{
    public class StandaloneInspectorPanel : EditorWindow
    {
        [SerializeReference]
        private AInspectorPanel m_inspectorPanel;

        public void SetElement(WorkflowElementKey elementKey)
        {
            if (elementKey.IsInvalid())
            {
                return;
            }

            IDataElement element = elementKey.GetElement();
            if (element == null)
            {
                return;
            }
            
            Type inspectorPanelType = DatastoresEditorCore.GetInspectorPanelType(element);
            m_inspectorPanel = Activator.CreateInstance(inspectorPanelType) as AInspectorPanel;
            if(m_inspectorPanel == null)
            {
                Debug.LogError($"[StandaloneInspectorPanel] Failed to create inspector panel for IGElement: {element.DisplayName}");
                return;
            }

            m_inspectorPanel.SetElement(elementKey);
            OnEnable();
        }
        
        private void OnEnable()
        {
            rootVisualElement.Clear();
            if (m_inspectorPanel == null)
            {
                return;
            }
            rootVisualElement.Add(m_inspectorPanel.GetPanel());
        }
        
        public void OnDisable()
        {
            m_inspectorPanel.SaveState();
        }
    }
}