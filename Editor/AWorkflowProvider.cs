using System;
using System.Collections.Generic;
using DatastoresDX.Runtime;

namespace DatastoresDX.Editor
{
    public class WorkflowProviderComparer : IComparer<AWorkflowProvider>
    {
        public int Compare(AWorkflowProvider x, AWorkflowProvider y)
        {
            if (x.IsSoloWorkflow && !y.IsSoloWorkflow)
                return -1;
            else if (!x.IsSoloWorkflow && y.IsSoloWorkflow)
                return 1;
            else
                return String.CompareOrdinal(x.DisplayName, y.DisplayName);
        }
    }
    
    public abstract class AWorkflowProvider
    {
        public abstract Uid Id { get; }
        public virtual string DisplayName => GetType().Name;
        public abstract bool IsSoloWorkflow { get; }
        
        protected abstract List<AWorkflow> LoadWorkflows();
        protected abstract AWorkflow HandleCreateNewWorkflow();
        protected abstract bool HandleDeleteWorkflow(Uid workflowId);
        
        private Dictionary<Uid, AWorkflow> m_workflows = new();

        public void Initialize()
        {
            List<AWorkflow> workflows = LoadWorkflows();
            foreach (AWorkflow workflow in workflows)
            {
                workflow.Initialize();
            }
            workflows.Sort(new WorkflowComparer());
            
            m_workflows.Clear();
            foreach (AWorkflow workflow in workflows)
            {
                m_workflows.Add(workflow.Id, workflow);
            }
        }
        
        public AWorkflow GetWorkflow(Uid workflowId)
        {
            if (!m_workflows.ContainsKey(workflowId))
            {
                return null;
            }

            return m_workflows[workflowId];
        }
        
        public List<AWorkflow> GetWorkflows()
        {
            List<AWorkflow> workflows = new List<AWorkflow>(m_workflows.Values);
            workflows.Sort(new WorkflowComparer());
            return workflows;
        }
        
        public bool ContainsWorkflow(Uid workflowId)
        {
            return m_workflows.ContainsKey(workflowId);
        }

        public AWorkflow CreateNewWorkflow()
        {
            AWorkflow workflow = HandleCreateNewWorkflow();
            if (workflow != null)
            {
                m_workflows.Add(workflow.Id, workflow);
                workflow.Initialize();
            }

            return workflow;
        }

        public bool DeleteWorkflow(Uid workflowId)
        {
            bool success = HandleDeleteWorkflow(workflowId);
            if (success)
            {
                m_workflows.Remove(workflowId);
            }

            return success;
        }
    }
}