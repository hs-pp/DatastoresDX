using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public abstract class ElementTreeNodeDrawer : VisualElement
    {
        public abstract void SetElement(WorkflowElementKey elementKey);
        public virtual void ResetElement() { }
    }
}