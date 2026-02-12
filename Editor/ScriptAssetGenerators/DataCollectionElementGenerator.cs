using UnityEngine;

namespace DatastoresDX.Editor.ScriptAssetGenerators
{
    [CreateAssetMenu(fileName = "NewElement", menuName = "Datastores/DataCollection Element C#", order = 100)]
    public class DataCollectionElementGenerator : ScriptAssetGenerator
    {
        protected override string GetTemplate()
        {
            return
                "using DatastoresDX.Runtime;\n" +
                "using DatastoresDX.Runtime.DataCollections;\n" +
                "\n" +
                "[DataElement(typeof(DATACOLLECTIONTYPE), \"<NAME>\")]\n" +
                "public class <NAME> : DataCollectionElement\n" +
                "{\n" +
                "\n" +
                "}";
        }
    }
}