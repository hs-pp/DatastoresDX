using System.IO;
using UnityEditor;

namespace DatastoresDX.Editor.ScriptAssetGenerators
{
    public class ScriptAssetProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string importedAssetPath in importedAssets)
            {
                ScriptAssetGenerator importedAsset = AssetDatabase.LoadAssetAtPath(importedAssetPath, typeof(ScriptAssetGenerator)) as ScriptAssetGenerator;
                if (importedAsset == null)
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(importedAssetPath);
                fileName = fileName.Trim();
                fileName = fileName.Replace(" ", "_");
                fileName = fileName.Replace("-", "_");
                string filePath = Path.GetDirectoryName(importedAssetPath);
                filePath = filePath.Replace(@"\", "/");

                StreamWriter sw = new StreamWriter(System.IO.Path.Combine(filePath, (fileName + ".cs")));
                sw.Write(importedAsset.GetFinalScript(fileName));
                sw.Close();

                AssetDatabase.DeleteAsset(importedAssetPath);

                AssetDatabase.Refresh();
            }
        }
    }
}