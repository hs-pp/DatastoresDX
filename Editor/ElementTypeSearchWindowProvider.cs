using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DatastoresDX.Editor
{
    public class ElementTypeSearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private List<ElementTypeDefinition> m_elementTypes = new();
        private Action<ElementTypeDefinition> m_onElementTypeSelected;

        public void Setup(List<ElementTypeDefinition> elementTypes, Action<ElementTypeDefinition> onSelected)
        {
            m_elementTypes = elementTypes;
            m_onElementTypeSelected = onSelected;
        }
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> entries = new List<SearchTreeEntry>();
            entries.Add(new SearchTreeGroupEntry(new GUIContent("All Nodes"), 0));
            foreach (ElementTypeDefinition elementType in m_elementTypes)
            {
                entries.Add(new SearchTreeEntry(new GUIContent(elementType.SearchWindowPath)){ level = 1, userData = elementType });
            }

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            m_onElementTypeSelected?.Invoke(SearchTreeEntry.userData as ElementTypeDefinition);
            return true;
        }
    }
}