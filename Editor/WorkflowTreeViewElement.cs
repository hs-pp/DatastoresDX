namespace DatastoresDX.Editor
{
    public abstract class BaseWorkflowTreeViewElement
    {
        public abstract string DisplayName { get; }
    }

    public class WorkflowTreeViewElement : BaseWorkflowTreeViewElement
    {
        public override string DisplayName => Workflow.DisplayName;
        public AWorkflow Workflow { get; }
        public AWorkflowProvider Provider { get; }

        public WorkflowTreeViewElement(AWorkflow workflow, AWorkflowProvider provider)
        {
            Workflow = workflow;
            Provider = provider;
        }
        
        public bool DeleteWorkflow()
        {
            return Provider.DeleteWorkflow(Workflow.Id);
        }
    }
    
    public class WorkflowProviderTreeViewElement : BaseWorkflowTreeViewElement
    {
        public override string DisplayName => Provider.DisplayName;
        public AWorkflowProvider Provider { get; }
        
        public WorkflowProviderTreeViewElement(AWorkflowProvider provider)
        {
            Provider = provider;
        }

        public AWorkflow CreateNewWorkflow()
        {
            return Provider.CreateNewWorkflow();
        }
    }

    public class SoloWorkflowTreeViewElement : BaseWorkflowTreeViewElement
    {
        public override string DisplayName => Provider.DisplayName;
        public AWorkflow Workflow { get; private set; }
        public AWorkflowProvider Provider { get; }

        public SoloWorkflowTreeViewElement(AWorkflow workflow, AWorkflowProvider provider)
        {
            Workflow = workflow;
            Provider = provider;
        }
        
        public AWorkflow CreateNewWorkflow()
        {
            if (Workflow == null)
            {
                Workflow = Provider.CreateNewWorkflow();
                return Workflow;
            }

            return null;
        }
    }
}