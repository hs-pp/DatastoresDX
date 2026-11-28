using System;
using System.Collections.Generic;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor.DataCollections
{
    [System.Serializable]
    [InspectorPanel(typeof(DataCollectionElement))]
    public class DefaultDataCollectionInspectorPanel : AInspectorPanel
    {
        [Serializable]
        private struct SoftRefPreviewOpenState
        {
            public Uid ElementId;
            public string PropertyPath;
        }

        private PropertyField m_propertyField;
        
        // Generic support for saving SoftRef preview state.
        [SerializeField]
        private List<SoftRefPreviewOpenState> m_softRefPreviewOpenStates = new();

        protected override VisualElement CreatePanel()
        {
            m_propertyField = new PropertyField();
            m_propertyField.RegisterCallback<SoftRefAttachedEvent>(OnSoftRefAttached);
            return m_propertyField;
        }

        protected override void OnSetElement(WorkflowElementKey elementKey)
        {
            DataCollectionElementWrapper element = elementKey.GetElement() as DataCollectionElementWrapper;
            if (element == null)
            {
                return;
            }

            m_propertyField.BindProperty(element.ElementSP);
        }

        private void OnSoftRefAttached(SoftRefAttachedEvent evt)
        {
            evt.StopPropagation();
            SoftRefElement field = evt.Field;
            Uid elementId = m_elementkey.ElementId;
            string propertyPath = field.RelativePropertyPath;

            field.PreviewOpen = m_softRefPreviewOpenStates.Exists(x => x.ElementId.Equals(elementId) && x.PropertyPath == propertyPath);
            field.PreviewOpenChanged += open => SetSoftRefPreviewOpen(elementId, propertyPath, open);
        }

        private void SetSoftRefPreviewOpen(Uid elementId, string propertyPath, bool open)
        {
            m_softRefPreviewOpenStates.RemoveAll(x => x.ElementId.Equals(elementId) && x.PropertyPath == propertyPath);
            if (open)
            {
                m_softRefPreviewOpenStates.Add(new SoftRefPreviewOpenState { ElementId = elementId, PropertyPath = propertyPath });
            }
        }
    }
}
