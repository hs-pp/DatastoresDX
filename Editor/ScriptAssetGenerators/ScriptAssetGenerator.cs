using UnityEngine;

namespace DatastoresDX.Editor.ScriptAssetGenerators
{
    public abstract class ScriptAssetGenerator : ScriptableObject
    {
        public static string NAME_TAG = "<NAME>";

        public string GetFinalScript(string name)
        {
            return GetTemplate().Replace(NAME_TAG, name);
        }

        protected abstract string GetTemplate();

    }
}