using System;
using System.Collections.Generic;
using DatastoresDX.Runtime;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public class ElementTreeNode : VisualElement
    {
        private const string VIEW_UXML = "DatastoresDX/ElementTreeNode";
        private const string DRAWER_AREA_TAG = "drawer-area";
        private const string CONTEXT_MENU_CATCHER_TAG = "context-menu-catcher";

        private VisualElement m_drawerArea;
        private VisualElement m_contextMenuCatcher;

        private WorkflowElementKey m_elementKey = WorkflowElementKey.Invalid;
        private ElementListPanel m_elementListPanel;
        private ElementTreeNodeDrawer m_elementTreeNodeDrawer;

        public ElementTreeNode()
        {
            CreateLayout();
        }

        private void CreateLayout()
        {
            var uxmlAsset = Resources.Load<VisualTreeAsset>(VIEW_UXML);
            uxmlAsset.CloneTree(this);

            m_drawerArea = this.Q<VisualElement>(DRAWER_AREA_TAG);
            m_contextMenuCatcher = this.Q<VisualElement>(CONTEXT_MENU_CATCHER_TAG);

            // Shitty hack because adding the manip to this class doesnt work. Probably just another trash TreeView gotcha.
            m_contextMenuCatcher.AddManipulator(
                new ContextualMenuManipulator(cmpe =>
                {
                    cmpe.menu.AppendAction("Create Element", (a) => { OnElementAdd(); });
                    cmpe.menu.AppendAction("Delete Element", (a) => { OnDeleteElement(); });
                }));
        }

        public void BindElementNode(WorkflowElementKey elementKey, ElementListPanel elementListPanel)
        {
            ResetElement();
            m_elementKey = elementKey;
            m_elementListPanel = elementListPanel;

            IDataElement element = elementKey.GetElement();
            Type elementTreeNodeDrawerType = DatastoresEditorCore.GetElementTreeNodeDrawerType(element);
            m_elementTreeNodeDrawer = Activator.CreateInstance(elementTreeNodeDrawerType) as ElementTreeNodeDrawer;
            m_elementTreeNodeDrawer.style.minHeight = 19;
            if (m_elementTreeNodeDrawer == null)
            {
                Debug.LogError($"[ElementTreeNode] Failed to create ElementTreeNodeDrawer for {elementKey.GetElement().GetType()}");
                return;
            }
            
            m_elementTreeNodeDrawer.style.flexGrow = 1;
            
            m_drawerArea.Add(m_elementTreeNodeDrawer);
            m_elementTreeNodeDrawer.SetElement(m_elementKey);
            
            // hack to allow text wrap to work correctly. Dammit Unity TreeView.
            parent.style.flexShrink = 1;
        }

        public void ResetElement()
        {
            m_elementKey = WorkflowElementKey.Invalid;
            m_drawerArea.Clear();
            m_elementTreeNodeDrawer = null;
        }

        private void OnElementAdd()
        {
            AWorkflow workflow = m_elementKey.GetWorkflow();
            ElementTypeSearchWindowProvider searchProvider = ScriptableObject.CreateInstance<ElementTypeSearchWindowProvider>();
            List<ElementTypeDefinition> elementTypes = workflow.GetElementTypes();
            if (elementTypes == null)
            {
                elementTypes = new List<ElementTypeDefinition>();
            }
            searchProvider.Setup(elementTypes, definition =>
            {
                Uid newId = workflow.AddElement(definition.ElementType, m_elementKey.ElementId);
                m_elementListPanel.HandleAfterElementAdd(newId);
            });
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Mouse.current.position.ReadValue())), searchProvider);
        }

        private void OnDeleteElement()
        {
            m_elementKey.GetWorkflow().DeleteElement(m_elementKey.ElementId);
            m_elementListPanel.HandleElementDelete(m_elementKey.ElementId);
        }
    }
}