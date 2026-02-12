using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DatastoresDX.Editor.DataCollections;
using DatastoresDX.Runtime;
using DatastoresDX.Runtime.DataCollections;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace DatastoresDX.Editor
{
    public class DataCollectionAnalyzeRule : AnalyzeRule
    {
        private class GroupDefinition
        {
            public string Name;
            public List<AssetDefinition> Assets;
            public DataCollection Owner; // The DataCollection this group represent. Can be null.

            public GroupDefinition(string name, DataCollection owner)
            {
                Name = name;
                Assets = new List<AssetDefinition>();
                Owner = owner;
            }

            public void AddAssetDef(AssetDefinition assetDef)
            {
                assetDef.Labels.Add(Owner.Id.ToString());
                Assets.Add(assetDef);
            }

            public void PrintEverything(StringBuilder stringBuilder)
            {
                stringBuilder.AppendLine($"{Name} (Count: {Assets.Count})");
                foreach (AssetDefinition asset in Assets)
                {
                    asset.PrintEverything(stringBuilder);
                }
            }
        }

        private class AssetDefinition
        {
            public string AddressableName;
            public string AssetGuid;
            public HashSet<string> Labels;

            public void PrintEverything(StringBuilder stringBuilder)
            {
                stringBuilder.Append($"   - {AddressableName} ");
                foreach (string label in Labels)
                {
                    stringBuilder.Append($"({label})");
                }
                stringBuilder.AppendLine();
            }
        }
        
        private enum AssetOperation : int
        {
            Unknown = 0,
            Add = 1,
            Move = 2,
            Remove = 3,
            Rename = 4,
            FixLabels = 5,
        }
        
        private class GroupAction
        {
            public string Name;
            public AssetOperation Operation = AssetOperation.Unknown;

            public GroupAction(string name, AssetOperation operation)
            {
                Name = name;
                Operation = operation;
            }
        }
        
        private class AssetAction
        {
            public string AddressableName;
            public string AssetGuid;
            public HashSet<string> Labels;
            public string GroupName;
            public AssetOperation Operation = AssetOperation.Unknown;
        }
        
        public override bool CanFix => true;
        public override string ruleName => "DataCollections Analyze Rules";
        private static string m_dataCollectionGroupNamePrefix = "[DataCollections-Group] ";
        private static string DataCollectionAddressablesPrefix => "DataCollection";
        private static string m_bundledAssetPrefix => "BundledAsset";
        private static string m_dependencyPrefix => "Dependency";
        
        private static string m_templateToUse = "Packed Assets"; // Group settings template to use for newly created groups.
        private static int m_minimumFileSize = 0; //10000; // Minimum file size to consider for asset bundles.
        private static string m_projectRoot = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets/".Length);

        private List<GroupAction> m_groupActions = new();
        private List<AssetAction> m_assetActions = new();

        public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            ClearAll();
            List<AnalyzeResult> results = new();
            
            if (!BuildUtility.CheckModifiedScenesAndAskToSave())
            {
                Debug.LogError("Cannot run Analyze with unsaved scenes");
                results.Add(new AnalyzeResult { resultName = ruleName + "Cannot run Analyze with unsaved scenes" });
                return results;
            }
            
            List<GroupDefinition> desiredGroups = new();
            
            // Collect DataCollections
            foreach (AWorkflowProvider provider in DatastoresEditorCore.GetWorkflowProviders())
            {
                if (provider is ABaseTemplateWorkflowProvider templateProvider)
                {
                    Type templateType = templateProvider.GetElementType();
                    if (typeof(DataCollection).IsAssignableFrom(templateProvider.GetElementType()))
                    {
                        DataCollectionAttribute attribute =
                            templateType.GetCustomAttribute<DataCollectionAttribute>();
                        if (attribute != null && attribute.RuntimeSupported)
                        {
                            foreach (AWorkflow workflow in provider.GetWorkflows())
                            {
                                DataCollection dataCollection = (workflow as DataCollectionWorkflow).DataCollection;
                                desiredGroups.Add(new GroupDefinition(GetDataCollectionGroupName(dataCollection), dataCollection));
                            }
                        }
                    }
                }
            }

            Dictionary<string, AssetDefinition> processedAssetGuids = new();
            
            // Collect assets to bundle from every DataCollectionElement
            foreach (GroupDefinition group in desiredGroups.Where(x => x.Owner != null))
            {
                DataCollection dataCollection = group.Owner;
                string dataCollectionPath = AssetDatabase.GetAssetPath(dataCollection);
                string dataCollectionGuid = AssetDatabase.AssetPathToGUID(dataCollectionPath);
                
                // Add the DataCollection -itself.
                AssetDefinition dataCollectionAsset = new AssetDefinition
                {
                    AddressableName = GetDataCollectionAddressableName(dataCollection),
                    AssetGuid = dataCollectionGuid,
                    Labels = new() { dataCollection.GetType().Name },
                };
                group.AddAssetDef(dataCollectionAsset);
                processedAssetGuids.Add(dataCollectionGuid, dataCollectionAsset);
                
                ProcessAssetDependencies(dataCollectionPath, group, dataCollection.Id, processedAssetGuids);
                
                foreach (DataCollectionElement element in dataCollection.GetAllElements())
                {
                    foreach (BundleAssetConfig bundleAssetConfig in element.GetAssetsToBundle())
                    {
                        // Do not process empty assets.
                        if (string.IsNullOrEmpty(bundleAssetConfig.AssetGuid))
                        {
                            continue;
                        }
                        
                        // Only ever add an asset once.
                        if (processedAssetGuids.ContainsKey(bundleAssetConfig.AssetGuid))
                        {
                            processedAssetGuids[bundleAssetConfig.AssetGuid].Labels.Add(element.Id.ToString());
                            continue;
                        }

                        HashSet<string> labels = new() { m_bundledAssetPrefix, element.Id.ToString() };
                        if (bundleAssetConfig.Labels != null)
                        {
                            foreach (string label in bundleAssetConfig.Labels)
                            {
                                labels.Add(label);
                            }
                        }

                        AssetDefinition assetDefinition = new AssetDefinition
                        {
                            AddressableName = GetBundledAssetAddresssableName(bundleAssetConfig),
                            AssetGuid = bundleAssetConfig.AssetGuid,
                            Labels = labels,
                        };
                        group.AddAssetDef(assetDefinition);
                        processedAssetGuids.Add(bundleAssetConfig.AssetGuid, assetDefinition);

                        ProcessAssetDependencies(bundleAssetConfig.AssetGuid, group, element.Id, processedAssetGuids);
                    }
                }
            }

            // Debug print everything.
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("Desired Addressables Groups:");
            foreach (GroupDefinition group in desiredGroups)
            {
                group.PrintEverything(stringBuilder);
            }
            Debug.Log(stringBuilder.ToString());

            // Get all addressable groups carrying the correct prefixes
            HashSet<string> existingGroups = new HashSet<string>();
            foreach (var group in settings.groups)
            {
                if (group.name.StartsWith(m_dataCollectionGroupNamePrefix))
                {
                    existingGroups.Add(group.name);
                }
            }
            
            // Diff the existing groups with the desired groups and create GroupActions.
            foreach (GroupDefinition desiredGroup in desiredGroups)
            {
                if (!existingGroups.Contains(desiredGroup.Name))
                {
                    m_groupActions.Add(new GroupAction(desiredGroup.Name, AssetOperation.Add));
                    results.Add(new AnalyzeResult() { resultName = "Create Group \"" + desiredGroup.Name + "\"" });
                }
            }
            foreach (var existingGroup in existingGroups)
            {
                if (!desiredGroups.Exists(x => x.Name == existingGroup))
                {
                    m_groupActions.Add(new GroupAction(existingGroup, AssetOperation.Remove));
                    results.Add(new AnalyzeResult() { resultName = "Remove Group \"" + existingGroup + "\"" });
                }
            }
            
            // Diff the existing assets with the desired assets and create AssetActions.
            foreach (GroupDefinition desiredGroup in desiredGroups)
            {
                foreach (AssetDefinition asset in desiredGroup.Assets)
                {
                    AddressableAssetEntry entry = settings.FindAssetEntry(asset.AssetGuid);
                    if (entry == null) // add operation
                    {
                        m_assetActions.Add(new AssetAction()
                        {
                            AddressableName = asset.AddressableName,
                            AssetGuid = asset.AssetGuid,
                            GroupName = desiredGroup.Name,
                            Labels = asset.Labels,
                            Operation = AssetOperation.Add,
                        });
                        
                        results.Add(new AnalyzeResult() { resultName = $"Add Asset: {asset.AddressableName}" });
                    }
                    else if (entry.address != asset.AddressableName) // rename operation
                    {
                        m_assetActions.Add(new AssetAction()
                        {
                            AddressableName = asset.AddressableName,
                            AssetGuid = asset.AssetGuid,
                            GroupName = desiredGroup.Name,
                            Labels = asset.Labels,
                            Operation = AssetOperation.Rename,
                        });
                        
                        results.Add(new AnalyzeResult() { resultName = $"Rename Asset: {entry.address} --> {asset.AddressableName}" });
                    }
                    else if (entry.parentGroup.Name != desiredGroup.Name) // move operation
                    {
                        m_assetActions.Add(new AssetAction()
                        {
                            AddressableName = asset.AddressableName,
                            AssetGuid = asset.AssetGuid,
                            GroupName = desiredGroup.Name,
                            Labels = asset.Labels,
                            Operation = AssetOperation.Move,
                        });
                        
                        results.Add(new AnalyzeResult() { resultName = $"Move Asset: ({asset.AddressableName}) {entry.parentGroup.Name} --> {desiredGroup.Name}" });
                    }
                    else if (!entry.labels.SetEquals(asset.Labels))
                    {
                        m_assetActions.Add(new AssetAction()
                        {
                            AddressableName = asset.AddressableName,
                            AssetGuid = asset.AssetGuid,
                            GroupName = desiredGroup.Name,
                            Labels = asset.Labels,
                            Operation = AssetOperation.FixLabels,
                        });
                        
                        results.Add(new AnalyzeResult() { resultName = $"FixLabels Asset: ({asset.AddressableName})" });
                    }
                }

                AddressableAssetGroup existingGroup = settings.FindGroup(desiredGroup.Name);
                if (existingGroup == null)
                {
                    continue;
                }
                
                List<AddressableAssetEntry> result = new List<AddressableAssetEntry>();
                existingGroup.GatherAllAssets(result, true, false, true);

                foreach (AddressableAssetEntry entry in result)
                {
                    if (entry.IsSubAsset)
                    {
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(entry.guid))
                    {
                        continue;
                    }

                    if (!processedAssetGuids.ContainsKey(entry.guid)) // remove operation
                    {
                        m_assetActions.Add(new AssetAction()
                        {
                            AssetGuid = entry.guid,
                            Operation = AssetOperation.Remove,
                        });
                        results.Add(new AnalyzeResult() { resultName = $"Remove Asset {entry.address}" });
                    }
                }
            }

            return results;
        }

        private void ProcessAssetDependencies(string assetPath, GroupDefinition groupDefinition, Uid ownerElementId, Dictionary<string, AssetDefinition> processedAssetGuids)
        {
            string[] dependencies = AssetDatabase.GetDependencies(assetPath);
            
            foreach (string dependencyPath in dependencies)
            {
                string dependencyGuid = AssetDatabase.AssetPathToGUID(dependencyPath);
                
                if (string.IsNullOrEmpty(dependencyGuid))
                {
                    return;
                }
                
                if(dependencyPath.EndsWith(".cs") || dependencyPath.EndsWith(".dll"))
                {
                    // Ignore scripts
                    continue;
                }
                
                // Only ever add an asset once.
                if (processedAssetGuids.ContainsKey(dependencyGuid))
                {
                    processedAssetGuids[dependencyGuid].Labels.Add(ownerElementId.ToString());
                    return;
                }                
                
                // Only create asset bundle for large files
                var diskPath = m_projectRoot + "/" + dependencyPath;
                var fileInfo = new System.IO.FileInfo(diskPath);
                if (fileInfo.Length > m_minimumFileSize)
                {
                    continue;
                }

                AssetDefinition assetDefinition = new AssetDefinition
                {
                    AddressableName = GetDependencyAddressableName(dependencyGuid),
                    AssetGuid = dependencyGuid,
                    Labels = new() { m_dependencyPrefix, ownerElementId.ToString() },
                };
                groupDefinition.AddAssetDef(assetDefinition);
                processedAssetGuids.Add(dependencyGuid, assetDefinition);
                
                ProcessAssetDependencies(dependencyPath, groupDefinition, ownerElementId, processedAssetGuids);
            }
        }
        
        public override void FixIssues(AddressableAssetSettings settings)
        {
            AddressableAssetGroupTemplate groupTemplate = settings.GroupTemplateObjects.Find(x => x.name == m_templateToUse) as AddressableAssetGroupTemplate;
            if (groupTemplate == null)
            {
                Debug.Log("Group template \"" + m_templateToUse + "\" not found. Aborting!");
                return;
            }

            // Add and remove groups
            foreach (GroupAction groupAction in m_groupActions)
            {
                if (groupAction.Operation == AssetOperation.Add)
                {
                    // No schema. Template instead.
                    AddressableAssetGroup newGroup = settings.CreateGroup(groupAction.Name, 
                        false, false, true, null, groupTemplate.GetTypes());
                    groupTemplate.ApplyToAddressableAssetGroup(newGroup);   
                }
                else if (groupAction.Operation == AssetOperation.Remove)
                {
                    AddressableAssetGroup existingGroup = settings.groups.Find(x => x.name == groupAction.Name);
                    if (existingGroup != null)
                    {
                        settings.RemoveGroup(existingGroup);
                    }
                }
            }
            
            // Collect current group names
            Dictionary<string, AddressableAssetGroup> groups = new();
            foreach (var group in settings.groups)
            {
                groups.Add(group.name, group);
            }
            
            // Add and remove assets
            foreach (AssetAction assetAction in m_assetActions)
            {
                // Remove operation
                if (assetAction.Operation == AssetOperation.Remove)
                {
                    AddressableAssetEntry entry = settings.FindAssetEntry(assetAction.AssetGuid);
                    if (entry != null)
                    {
                        settings.RemoveAssetEntry(assetAction.AssetGuid);
                    }

                    return;
                }
                
                // Other operations
                if (!string.IsNullOrEmpty(assetAction.GroupName) && !groups.ContainsKey(assetAction.GroupName))
                {
                    continue;
                }
                
                AddressableAssetGroup group = groups[assetAction.GroupName];
                
                if (assetAction.Operation == AssetOperation.Add || assetAction.Operation == AssetOperation.Move || 
                    assetAction.Operation == AssetOperation.Rename || assetAction.Operation == AssetOperation.FixLabels)
                {
                    AddressableAssetEntry entry = settings.CreateOrMoveEntry(assetAction.AssetGuid, group);
                    entry.SetAddress(assetAction.AddressableName);
                    entry.parentGroup = group;
                    entry.labels.Clear();
                    foreach (string label in assetAction.Labels)
                    {
                        entry.SetLabel(label, true, true);
                    }
                }
            }
            
            ClearAll();
        }
        
        private string GetDataCollectionGroupName(DataCollection dataCollection)
        {
            return $"{m_dataCollectionGroupNamePrefix}[{dataCollection.Id}] {dataCollection.DisplayName}";;
        }

        public static string GetDataCollectionAddressableName(DataCollection dataCollection)
        {
            return $"{DataCollectionAddressablesPrefix}/{dataCollection.Id}";
        }

        private static string GetBundledAssetAddresssableName(BundleAssetConfig bundleAssetConfig)
        {
            //return $"{m_bundledAssetPrefix}/{AssetDatabase.GUIDToAssetPath(bundleAssetConfig.AssetGuid)}";
            return $"{m_bundledAssetPrefix}/{bundleAssetConfig.AssetGuid}";
        }

        private static string GetDependencyAddressableName(string objectGuid)
        {
            //return $"{m_dependencyPrefix}/{AssetDatabase.GUIDToAssetPath(objectGuid)}";
            return $"{m_dependencyPrefix}/{objectGuid}";
        }
        
        private void ClearAll()
        {
            ClearAnalysis();
            m_groupActions.Clear();
            m_assetActions.Clear();
        }
        
        [InitializeOnLoad]
        class RegisterBuildBundleLayout
        {
            static RegisterBuildBundleLayout()
            {
                AnalyzeSystem.RegisterNewRule<DataCollectionAnalyzeRule>();
            }
        }
    }
}