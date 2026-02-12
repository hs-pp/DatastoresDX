using DatastoresDX.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    [UxmlElement]
    public partial class UidElement : VisualElement
    {
        private const string VIEW_UXML = "DatastoresDX/UidElement";
        private const string ID1_LABEL_TAG = "id1-label";
        
        private Label m_id1Label;

        private Uid m_id;
        
        public UidElement(Uid id) : this()
        {
            SetUuid(id);
        }

        public UidElement()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);
            
            m_id1Label = this.Q<Label>(ID1_LABEL_TAG);
            
            this.AddManipulator(new ContextualMenuManipulator(cmpe =>
            {
                cmpe.menu.AppendAction("Copy to Clipboard", dma =>
                {
                    EditorGUIUtility.systemCopyBuffer = m_id.ToString();
                });
            }));
        }

        public void SetUuid(Uid id)
        {
            m_id = id;
            if (m_id.IsInvalid())
            {
                m_id1Label.text = "------";
            }
            else
            {
                m_id1Label.text = Uid.ToBase62(m_id);
            }
        }
    }
}