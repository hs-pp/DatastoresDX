using DatastoresDX.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    [CustomPropertyDrawer(typeof(Uid))]
    public class UidPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new UidField(property, new UidElement(Uid.FromSerializedProperty(property)));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.LabelField(position, Uid.FromSerializedProperty(property).ToString());
        }
    }
    
    public class UidField : BaseField<DataReference<IDataElement>>
    {
        public UidField(SerializedProperty dataReferenceSP, UidElement visualInput) : base(dataReferenceSP.displayName, visualInput)
        {
            AddToClassList(alignedFieldUssClassName);
        }
    }
}