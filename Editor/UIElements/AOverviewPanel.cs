using System;
using DatastoresDX.Runtime;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    [Serializable]
    public abstract class AOverviewPanel
    {
        protected VisualElement m_panel;
        [SerializeField]
        protected Uid m_workflowId = Uid.Invalid;
        
        public VisualElement GetPanel()
        {
            if (m_panel == null)
            {
                m_panel = CreatePanel();
                LoadElement();
            }
            return m_panel;
        }

        public void SetWorkflow(Uid workflowId)
        {
            m_workflowId = workflowId;
            GetPanel();
            AWorkflow workflow = DatastoresEditorCore.GetWorkflow(workflowId);
            OnSetWorkflow(workflow);
        }

        public void LoadElement()
        {
            if (!m_workflowId.IsInvalid())
            {
                SetWorkflow(m_workflowId);
            }
        }

        protected abstract VisualElement CreatePanel();
        
        /// <summary>
        /// If this is a DataCollection OverviewPanel, the IWorkflow will always be of type DataCollectionWorkflow.
        /// </summary>
        protected abstract void OnSetWorkflow(AWorkflow workflow);
        public virtual void SaveState() { }
    }
}