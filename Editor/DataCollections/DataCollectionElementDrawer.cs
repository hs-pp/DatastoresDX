using System;
using System.Reflection;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor.DataCollections
{
    [CustomPropertyDrawer(typeof(DataCollectionElement), true)]
    public class DataCollectionElementPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement element = new VisualElement();
            element.name = GetType().Name;
            element.style.flexGrow = 1;
            PopulateElementWithDefaultProperties(property, element);
            return element;
        }
        
        /// <summary>
        /// By default using a property drawer on a DataCollectionElement will also show a foldout wrapping the whole thing
        /// because elements are always in the elements list in a DataCollection.
        /// Manually looping through properties will avoid showing the foldout. 
        /// </summary>
        public static void PopulateElementWithDefaultProperties(SerializedProperty dataCollectionElementSP, VisualElement parentElement)
        {
            DataCollectionElementHeader header = new DataCollectionElementHeader();
            header.Bind(dataCollectionElementSP);
            parentElement.Add(header);
            
            SerializedProperty property = dataCollectionElementSP.Copy();
            SerializedProperty endProperty = property.GetEndProperty();
            property.NextVisible(true);
            do
            {
                if (property.name == DataCollectionElement.DisplayName_VarName || property.name == DataCollectionElement.Id_VarName)
                {
                    continue;
                }
                
                PropertyField propField = new PropertyField();
                propField.BindProperty(property);
                parentElement.Add(propField);
                MethodInfo dynMethod = typeof(SerializedProperty).GetMethod("GetFullyQualifiedTypenameForCurrentTypeTreeInternal", 
                    BindingFlags.NonPublic | BindingFlags.Instance);

                
                Assembly assembly = Assembly.Load("UnityEditor");
                Type internalType = assembly.GetType("UnityEditor.ScriptAttributeUtility");
                MethodInfo method = internalType.GetMethod("GetFieldInfoFromProperty", BindingFlags.NonPublic | BindingFlags.Static);

                object[] parameters = { property, null }; // Initialize the out parameter with null

                var sdfa = method.Invoke(null, parameters);
                var fi = parameters[1];
                
                
            } while (property.NextVisible(false) && !SerializedProperty.EqualContents(property, endProperty));
            
        }
        
    }
}