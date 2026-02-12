using UnityEngine;

namespace DatastoresDX.Editor.ScriptAssetGenerators
{
    [CreateAssetMenu(fileName = "NewDataCollection", menuName = "Datastores/DataCollection C#", order = 100)]
    public class DataAssetGenerator : ScriptAssetGenerator
    {
        protected override string GetTemplate()
        {
            return
                "using DatastoresDX.Runtime.DataCollections;\n" +
                "\n" +
                "[DataCollection(true)]\n" +
                "public class <NAME> : DataCollection\n" +
                "{\n" +
                "\n" +
                "}";
        }
    }
}