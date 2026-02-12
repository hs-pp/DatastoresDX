using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DatastoresDX.Runtime;
using UnityEngine;
using Random = System.Random;

namespace DatastoresDX.Editor
{
    public static class DatastoresEditorCore
    {
        private static List<AWorkflowProvider> m_workflowProviders = new(); // Sorted
        private static List<AWorkflowProvider> m_runtimeAvailableWorkflowProviders = new();
        private static List<AWorkflowProvider> m_editorOnlyWorkflowProviders = new();
        private static Dictionary<Type, List<ElementTypeDefinition>> m_elementTypeLookup = new(); // Sorted
        private static Dictionary<Type, Type> m_overviewPanelTypeLookup = new();
        private static Dictionary<Type, Type> m_inspectorPanelTypeLookup = new();
        private static Dictionary<Type, Type> m_elementTreeNodeDrawerTypeLookup = new();
        private static Dictionary<Type, string> m_iconPathLookup = new();
        private static HashSet<Uid> m_existingUids = new();

        public static Action OnReloaded;
        public static Action<Uid> OnWorkflowProviderUpdated;
        public static Action<Uid> OnWorkflowUpdated;
        
        static DatastoresEditorCore()
        {
            Initialize();
        }
        
        private static void Initialize()
        {
            m_workflowProviders.Clear();
            m_elementTypeLookup.Clear();
            m_overviewPanelTypeLookup.Clear();
            m_inspectorPanelTypeLookup.Clear();
            m_elementTreeNodeDrawerTypeLookup.Clear();
            m_iconPathLookup.Clear();
            m_existingUids.Clear();
            
            Dictionary<Type, Type> elementTypeToTemplateProviderTypes = new();
            WorkflowProviderComparer workflowProviderComparer = new WorkflowProviderComparer();
            ElementTypeDefinitionComparer elementTypeDefinitionComparer = new ElementTypeDefinitionComparer();
            
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract || type.IsGenericType)
                    {
                        continue;
                    }
                    
                    if (typeof(AWorkflowProvider).IsAssignableFrom(type))
                    {
                        if (typeof(ABaseTemplateWorkflowProvider).IsAssignableFrom(type))
                        {
                            Type baseType = type.BaseType;
                            if (baseType != null && baseType.IsGenericType && baseType.GetGenericTypeDefinition().IsAssignableFrom(typeof(ATemplateWorkflowProvider<>))) // We're only going one layer deep.
                            {
                                elementTypeToTemplateProviderTypes.Add(baseType.GetGenericArguments()[0], type);
                            }
                        }
                        else
                        {
                            AWorkflowProvider provider = Activator.CreateInstance(type) as AWorkflowProvider;

                            // Insert sorted
                            var index = m_workflowProviders.BinarySearch(provider, workflowProviderComparer);
                            if (index < 0)
                            {
                                index = ~index;
                            }

                            m_workflowProviders.Insert(index, provider);
                        }
                    }
                    
                    else if (typeof(IDataElement).IsAssignableFrom(type))
                    {
                        DataElementAttribute attribute = type.GetCustomAttribute<DataElementAttribute>();
                        if (attribute == null)
                        {
                            continue;
                        }

                        if (attribute.WorkflowType != null)
                        {
                            if (!m_elementTypeLookup.ContainsKey(attribute.WorkflowType))
                            {
                                m_elementTypeLookup.Add(attribute.WorkflowType, new());
                            }
                            
                            ElementTypeDefinition newTypeDefinition = new ElementTypeDefinition()
                            {
                                SearchWindowPath = attribute.CreatePath,
                                ElementType = type 
                            };
                            
                            // Insert sorted
                            var index = m_elementTypeLookup[attribute.WorkflowType].BinarySearch(newTypeDefinition, elementTypeDefinitionComparer);
                            if (index < 0)
                            {
                                index = ~index;
                            }
                            m_elementTypeLookup[attribute.WorkflowType].Insert(index, newTypeDefinition);
                        }

                        if (!string.IsNullOrEmpty(attribute.IconPath))
                        {
                            m_iconPathLookup.Add(type, attribute.IconPath);
                        }
                    }
                    
                    else if (typeof(AOverviewPanel).IsAssignableFrom(type))
                    {
                        OverviewPanelAttribute attribute = type.GetCustomAttribute<OverviewPanelAttribute>();
                        if (attribute != null && attribute.WorkflowType != null &&
                            (typeof(IDataElement).IsAssignableFrom(attribute.WorkflowType)))
                        {
                            m_overviewPanelTypeLookup.Add(attribute.WorkflowType, type);
                        }
                    }
                    
                    else if (typeof(AInspectorPanel).IsAssignableFrom(type))
                    {
                        InspectorPanelAttribute attribute = type.GetCustomAttribute<InspectorPanelAttribute>();
                        if (attribute != null && attribute.ElementType != null &&
                            typeof(IDataElement).IsAssignableFrom(attribute.ElementType))
                        {
                            m_inspectorPanelTypeLookup.Add(attribute.ElementType, type);
                        }
                    }
                    
                    else if (typeof(ElementTreeNodeDrawer).IsAssignableFrom(type))
                    {
                        ElementTreeNodeDrawerAttribute attribute =
                            type.GetCustomAttribute<ElementTreeNodeDrawerAttribute>();
                        if (attribute != null && attribute.ElementType != null)
                        {
                            if (typeof(IDataElement).IsAssignableFrom(attribute.ElementType))
                            {
                                m_elementTreeNodeDrawerTypeLookup.Add(attribute.ElementType, type);
                            }
                        }
                    }
                }
            }
            
            List<Type> templateElementTypes = elementTypeToTemplateProviderTypes.Keys.ToList();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    Type templateElementType = templateElementTypes.Find(x => x.IsAssignableFrom(type));
                    if (templateElementType != null && type != templateElementType)
                    {
                        Type templateProviderType = elementTypeToTemplateProviderTypes[templateElementType];
                        ABaseTemplateWorkflowProvider constructedProvider = Activator.CreateInstance(templateProviderType) as ABaseTemplateWorkflowProvider;
                        constructedProvider.SetElementType(type);
                        
                        // insert sorted
                        var index = m_workflowProviders.BinarySearch(constructedProvider, workflowProviderComparer);
                        if (index < 0)
                        {
                            index = ~index;
                        }

                        m_workflowProviders.Insert(index, constructedProvider);
                    }
                }
            }
            
            foreach (AWorkflowProvider provider in m_workflowProviders)
            {
                provider.Initialize();
                if (Datastores.GetPipelineProvider(provider.Id) != null)
                {
                    m_runtimeAvailableWorkflowProviders.Add(provider);
                }
                else
                {
                    m_editorOnlyWorkflowProviders.Add(provider);
                }
            }

            foreach (AWorkflowProvider provider in m_workflowProviders)
            {
                foreach (AWorkflow workflow in provider.GetWorkflows())
                {
                    foreach (IDataElement element in workflow.GetAllElements())
                    {
                        if (m_existingUids.Contains(element.Id))
                        {
                            Debug.Log($"Duplicate Uid found: {element.Id}");
                            continue;
                        }
                        
                        m_existingUids.Add(element.Id);
                    }
                }
            }
        }
        
        public static List<AWorkflowProvider> GetWorkflowProviders()
        {
            return m_workflowProviders;
        }
        
        public static List<AWorkflowProvider> GetRuntimeAvailableProviders()
        {
            return m_runtimeAvailableWorkflowProviders;
        }
        
        public static List<AWorkflowProvider> GetEditorOnlyProviders()
        {
            return m_editorOnlyWorkflowProviders;
        }
        
        public static AWorkflowProvider GetWorkflowProvider(Uid providerId)
        {
            return m_workflowProviders.Find(x => x.Id.Equals(providerId));
        }
        public static AWorkflowProvider GetTemplateWorkflowProviderByTemplateWorkflowItemType(Type templateWorkflowItemType)
        {
            return m_workflowProviders.Find(x => x is ABaseTemplateWorkflowProvider templateProvider && templateProvider.GetElementType() == templateWorkflowItemType);
        }
        
        public static AWorkflow GetWorkflow(Uid workflowId)
        {
            if (workflowId.IsInvalid())
            {
                return null;    
            }

            foreach (AWorkflowProvider provider in m_workflowProviders)
            {
                AWorkflow workflow = provider.GetWorkflow(workflowId);
                if (workflow != null)
                {
                    return workflow;
                }
            }

            return null;
        }
        
        public static List<ElementTypeDefinition> GetElementTypes(Type workflowType)
        {
            List<ElementTypeDefinition> toReturn = new();
            while (workflowType != null)
            {
                if (m_elementTypeLookup.TryGetValue(workflowType, out var elementTypeDefinitions))
                {
                    toReturn.AddRange(elementTypeDefinitions);
                }
                
                workflowType = workflowType.BaseType;
            }

            return toReturn;
        }
        
        public static Type GetOverviewPanelType(AWorkflow workflow)
        {
            Type typeToFind = workflow.GetType();
            if (workflow is ILookupTypeOverride typeOverride)
            {
                typeToFind = typeOverride.LookupType;
            }
            
            while (typeToFind != null)
            {
                if (m_overviewPanelTypeLookup.TryGetValue(typeToFind, out var overviewPanelType))
                {
                    return overviewPanelType;
                }

                typeToFind = typeToFind.BaseType;
            }

            return typeof(DefaultOverviewPanel);
        }
        
        public static Type GetInspectorPanelType(IDataElement element)
        {
            Type typeToFind = element.GetType();
            if (element is ILookupTypeOverride typeOverride)
            {
                typeToFind = typeOverride.LookupType;
            }
            
            while (typeToFind != null)
            {
                if(m_inspectorPanelTypeLookup.TryGetValue(typeToFind, out var inspectorPanelType))
                {
                    return inspectorPanelType;
                }
                typeToFind = typeToFind.BaseType;
            }    
            
            return typeof(DefaultInspectorPanel);
        }

        public static Type GetElementTreeNodeDrawerType(IDataElement element)
        {
            Type typeToFind = element.GetType();
            if (element is ILookupTypeOverride typeOverride)
            {
                typeToFind = typeOverride.LookupType;
            }
            
            while (typeToFind != null)
            {
                if(m_elementTreeNodeDrawerTypeLookup.TryGetValue(typeToFind, out var treeNodeDrawerType))
                {
                    return treeNodeDrawerType;
                }
                typeToFind = typeToFind.BaseType;
            }
            return typeof(DefaultElementTreeNodeDrawer);
        }
        
        public static string GetIconPath(Type elementType)
        {
            while (elementType != null)
            {
                if (m_iconPathLookup.ContainsKey(elementType))
                {
                    return m_iconPathLookup[elementType];
                }

                elementType = elementType.BaseType;
            }
            return "DatastoresDX/GenericFileIcon"; // Default icon.
        }

        public static void Reload()
        {
            foreach(AWorkflowProvider provider in m_workflowProviders)
            {
                provider.Initialize();
            }
            OnReloaded?.Invoke();
        }
        
        public static void NotifyWorkflowProviderUpdated(Uid providerId)
        {
            OnWorkflowProviderUpdated?.Invoke(providerId);
        }
        
        public static void NotifyWorkflowUpdated(Uid workflowId)
        {
            OnWorkflowUpdated?.Invoke(workflowId);
        }

        public static WorkflowElementKey GetDataElementKey(Uid id, bool runtimeDataOnly = false)
        {
            List<AWorkflowProvider> providers =
                runtimeDataOnly ? GetRuntimeAvailableProviders() : m_workflowProviders;
            foreach (AWorkflowProvider provider in providers)
            {
                List<AWorkflow> workflows = provider.GetWorkflows();
                foreach (AWorkflow workflow in workflows)
                {
                    IDataElement element = workflow.GetElementById(id);
                    if (element != null)
                    {
                        return new WorkflowElementKey(workflow.Id, id);
                    }
                }
            }

            return WorkflowElementKey.Invalid;
        }

        /// <summary>
        /// Honestly this is basically a crime against humanity.
        /// Just ransack the whole library to find our data elements. Probably terrible performance.
        /// Thankfully it's editor only.
        /// </summary>
        private static Dictionary<Type, List<WorkflowElementKey>> m_cachedDataElementsOfTypeLookup = new();
        public static List<WorkflowElementKey> GetAllDataElementsOfType(Type desiredType)
        {
            if (m_cachedDataElementsOfTypeLookup.ContainsKey(desiredType))
            {
                return m_cachedDataElementsOfTypeLookup[desiredType];
            }
            
            HashSet<Type> workflowTypes = new();
            // First figure out which workflow types even allow this DataElement type.
            foreach (Type workflowType in m_elementTypeLookup.Keys)
            {
                foreach (ElementTypeDefinition elementTypeDefinition in m_elementTypeLookup[workflowType])
                {
                    if (desiredType == elementTypeDefinition.ElementType ||
                        elementTypeDefinition.ElementType.IsSubclassOf(desiredType))
                    {
                        workflowTypes.Add(workflowType);
                        break;
                    }
                }
            }
            
            // Next Gather all existing workflows of those types.
            List<AWorkflow> validWorkflows = new();
            foreach (AWorkflowProvider provider in GetWorkflowProviders())
            {
                foreach (AWorkflow workflow in provider.GetWorkflows())
                {
                    Type workflowType = workflow.GetType();
                    if (workflow is ILookupTypeOverride typeOverride)
                    {
                        workflowType = typeOverride.LookupType;
                    }
                    
                    foreach (Type validWorkflowType in workflowTypes)
                    {
                        if (workflowType == validWorkflowType || validWorkflowType.IsSubclassOf(workflowType))
                        {
                            validWorkflows.Add(workflow);
                            break;
                        }
                    }
                }
            }
            
            // Loop through all elements and collect those of the correct type.
            List<WorkflowElementKey> matches = new();
            foreach (AWorkflow workflow in validWorkflows)
            {
                foreach (IDataElement element in workflow.GetAllElements())
                {
                    Type dataElementType = element.GetType();
                    if (element is ILookupTypeOverride typeOverride)
                    {
                        dataElementType = typeOverride.LookupType;
                    }

                    if (dataElementType == desiredType || dataElementType.IsSubclassOf(desiredType))
                    {
                        matches.Add(new WorkflowElementKey(workflow.Id, element.Id));
                    }
                }
            }
            
            m_cachedDataElementsOfTypeLookup.Add(desiredType, matches);
            
            // return.
            return matches;
        }

        public static void PrintAllData()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append("Datastores Editor Core Initialized.");
            
            stringBuilder.Append("\n\nWorkflowProviders:");
            foreach (AWorkflowProvider provider in m_workflowProviders)
            {
                if (provider is ABaseTemplateWorkflowProvider templateProvider)
                {
                    stringBuilder.Append($"\n - {provider.GetType().Name}<{templateProvider.GetElementType()}>");
                }
                else
                {
                    stringBuilder.Append($"\n - {provider.GetType().Name}");
                }
            }

            stringBuilder.Append("\n\nElementTypeLookup:");
            foreach(Type workflowType in m_elementTypeLookup.Keys)
            {
                stringBuilder.Append("\n  " + workflowType.Name);
                foreach (ElementTypeDefinition elementTypeDefinition in m_elementTypeLookup[workflowType])
                {
                    stringBuilder.Append($"\n   - {elementTypeDefinition.ElementType.Name} ({elementTypeDefinition.SearchWindowPath})");
                }
            }
            
            stringBuilder.Append("\n\nOverviewPanelTypeLookup:");
            foreach (Type workflowType in m_overviewPanelTypeLookup.Keys)
            {
                stringBuilder.Append($"\n - {workflowType.Name} -> {m_overviewPanelTypeLookup[workflowType].Name}");
            }
            
            stringBuilder.Append("\n\nInspectorPanelTypeLookup:");
            foreach (Type elementType in m_inspectorPanelTypeLookup.Keys)
            {
                stringBuilder.Append($"\n - {elementType.Name} -> {m_inspectorPanelTypeLookup[elementType].Name}");
            }
            
            stringBuilder.Append("\n\nElementTreeNodeDrawerTypeLookup:");
            foreach (Type elementType in m_elementTreeNodeDrawerTypeLookup.Keys)
            {
                stringBuilder.Append($"\n - {elementType.Name} -> {m_elementTreeNodeDrawerTypeLookup[elementType].Name}");
            }
            
            stringBuilder.Append("\n\nIconPathLookup:");
            foreach (Type elementType in m_iconPathLookup.Keys)
            {
                stringBuilder.Append($"\n - {elementType.Name} -> {m_iconPathLookup[elementType]}");
            }
            
            stringBuilder.Append("\n");

            Debug.Log(stringBuilder.ToString());
        }

        public static Uid CrateUniqueId()
        {
            Random rand = new System.Random();
            Uid newId = new Uid(NextInt32(rand));

            int retryCount = 1;
            while (m_existingUids.Contains(newId) || newId.Value == 0)
            {
                newId = new Uid(NextInt32(rand));
                retryCount++;

                if (retryCount >= 100)
                {
                    break;
                }
            }

            if (retryCount > 1)
            {
                Debug.LogWarning($"It took {retryCount} tries to create a new Uid for {newId}");
                if (retryCount >= 100)
                {
                    throw new Exception($"Failed to create new Uid. Too many tries.");
                }
            }
            
            //Debug.Log($"New Uid for {newId}. Total is now {m_existingUids.Count}");
            m_existingUids.Add(newId);
            return newId;
        }

        public static void DestroyUid(Uid uid)
        {
            m_existingUids.Remove(uid);
        }
        
        private static Int32 NextInt32(Random rnd)
        {
            var buffer = new byte[sizeof(Int32)];
            rnd.NextBytes(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }
    }
}