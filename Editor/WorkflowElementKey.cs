using System;
using DatastoresDX.Runtime;
using UnityEngine;

namespace DatastoresDX.Editor
{
    /// <summary>
    /// Only used in Editor. At runtime we don't have the concept of a workflow and only need the Uid of the element.
    /// </summary>
    [Serializable]
    public struct WorkflowElementKey
    {
        [SerializeField]
        private Uid m_workflowId;
        public Uid WorkflowId => m_workflowId;
        [SerializeField]
        private Uid m_elementId;
        public Uid ElementId => m_elementId;
 
        public WorkflowElementKey(Uid workflowId, Uid elementId)
        {
            m_workflowId = workflowId;
            m_elementId = elementId;
        }
 
        public static WorkflowElementKey Invalid => new (Uid.Invalid, Uid.Invalid);
        public bool IsInvalid()
        {
            return m_workflowId.IsInvalid() || m_elementId.IsInvalid();
        }

        public AWorkflow GetWorkflow()
        {
            return DatastoresEditorCore.GetWorkflow(m_workflowId);
        }

        public IDataElement GetElement()
        {
            return GetWorkflow()?.GetElementById(m_elementId);
        }

        public override string ToString()
        {
            return $"WorkflowElementKey [Workflow: {m_workflowId} Element: {m_elementId}]";
        }
    }
}