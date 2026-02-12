using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DatastoresDX.Editor
{
    public class FiltersPopupContent : PopupWindowContent
    {
        public override void OnGUI(Rect rect) { }

        // public override Vector2 GetWindowSize()
        // {
        //     return new Vector2(300, 360);
        // }

        public override void OnOpen()
        {
            Label label = new Label("Filters leggo");
            editorWindow.rootVisualElement.Add(label);
        }
    }
}