using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DatastoresDX.Runtime.DataCollections;
using UnityEngine;

namespace DatastoresDX.Runtime
{
    public static class Datastores
    {
        private static List<APipelineProvider> m_pipelineProviders = new();
        private static List<APipeline> m_pipelines = new();
        private static Dictionary<Uid, APipeline> m_elementIdToPipeline = new();
        
        private static Dictionary<Type, List<IDataElement>> m_typeToIds = new(); // TODO: Possibly use TypeCache?
        public static bool IsInitialized { get; private set; }
        
        static Datastores()
        {
            LoadPipelineProviders();
            // Need to also call InitializePipelineProviders to be considered fully loaded.
            // But leave this here so it gets called at editor time too.
        }
        
        // // We're loading this from SystemCore currently.
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        // private static void OnBeforeSceneLoadRuntimeMethod()
        // {
        //     _ = InitializePipelines();
        // }

        private static void LoadPipelineProviders()
        {
            m_pipelineProviders.Clear();

            Dictionary<Type, Type> elementTypeToTemplateProviderTypes = new();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsClass || type.IsAbstract || type.IsGenericType)
                    {
                        continue;
                    }
                    
                    if(typeof(APipelineProvider).IsAssignableFrom(type))
                    {
                        if(typeof(ABaseTemplatePipelineProvider).IsAssignableFrom(type))
                        {
                            Type baseType = type.BaseType;
                            if (baseType != null && baseType.IsGenericType && baseType.GetGenericTypeDefinition().IsAssignableFrom(typeof(ATemplatePipelineProvider<>))) // We're only going one layer deep.
                            {
                                elementTypeToTemplateProviderTypes.Add(baseType.GetGenericArguments()[0], type);
                            }
                        }

                        else
                        {
                            m_pipelineProviders.Add((APipelineProvider)Activator.CreateInstance(type));
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
                        ABaseTemplatePipelineProvider constructedProvider = Activator.CreateInstance(templateProviderType) as ABaseTemplatePipelineProvider;
                        constructedProvider.SetElementType(type);
                        if (constructedProvider.ShouldCreateProvider)
                        {
                            m_pipelineProviders.Add(constructedProvider);
                        }
                    }
                }
            }
        }

        public static async Task InitializePipelines()
        {
            if (IsInitialized)
            {
                return;
            }
            
            Debug.Log("[Datastores] Initializing Datastores Runtime.");

            // Load ElementId to Provider map.
            foreach (APipelineProvider provider in m_pipelineProviders)
            {
                await provider.Initialize();
                foreach (APipeline pipeline in provider.GetPipelines())
                {
                    m_pipelines.Add(pipeline);
                    foreach (IDataElement element in pipeline.GetElements())
                    {
                        m_elementIdToPipeline.Add(element.Id, pipeline);
                        Type type = element.GetType();
                        if (!m_typeToIds.ContainsKey(type))
                        {
                            m_typeToIds.Add(type, new());
                        }
                        m_typeToIds[type].Add(element);
                    }
                }
            }
            
            IsInitialized = true;
            m_onInitialized?.Invoke();
            m_onInitialized = null;
        }
        
        public static APipelineProvider GetPipelineProvider(Uid providerId)
        {
            foreach (APipelineProvider provider in m_pipelineProviders)
            {
                if (provider.Id.Equals(providerId))
                {
                    return provider;
                }
            }
            return null;
        }

        public static List<APipelineProvider> GetPipelineProvidersOfType<T>() where T : APipelineProvider
        {
            List<APipelineProvider> pipelineProviders = new();
            foreach (APipelineProvider provider in m_pipelineProviders)
            {
                if (typeof(T).IsAssignableFrom(provider.GetType()))
                {
                    pipelineProviders.Add(provider);
                }
            }

            return pipelineProviders;
        }

        public static List<APipeline> GetPipelinesOfType<T>() where T : APipeline
        {
            List<APipeline> pipelines = new();
            foreach (APipeline pipeline in m_pipelines)
            {
                if (typeof(T).IsAssignableFrom(pipeline.GetType()))
                {
                    pipelines.Add(pipeline);
                }
            }

            return pipelines;
        }

        // Not ideal to be coupled to DataCollections but it's quick and easy.
        public static List<T> GetDataCollectionsOfType<T>() where T : DataCollection
        {
            List<T> dataCollections = new();
            foreach (APipeline pipeline in m_pipelines)
            {
                if (pipeline is DataCollectionPipeline dataCollectionPipeline)
                {
                    if (typeof(T).IsAssignableFrom(dataCollectionPipeline.DataCollection.GetType()))
                    {
                        dataCollections.Add((T)dataCollectionPipeline.DataCollection);
                    }
                }
            }

            return dataCollections;
        }
        
        public static List<T> GetElementsOfType<T>() where T : IDataElement
        {
            if (!IsInitialized)
            {
                Debug.LogError("[Datastores] Trying to access element before Datastores is loaded!");
                return null;
            }
            
            List<T> elements = new();
            foreach (Type type in m_typeToIds.Keys)
            {
                if (typeof(T).IsAssignableFrom(type))
                {
                    foreach (IDataElement element in m_typeToIds[type])
                    {
                        elements.Add((T)element);
                    }
                }
            }

            return elements;
        }
        
        public static IDataElement GetElement(Uid elementId)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[Datastores] Trying to access element before Datastores is loaded!");
                return null;
            }
            
            if (m_elementIdToPipeline.TryGetValue(elementId, out APipeline pipeline))
            {
                return pipeline.GetElement(elementId);
            }
            return null;
        }
        
        private static Action m_onInitialized;
        public static void OnInitializedOrCallImmediately(Action onInit)
        {
            if (IsInitialized)
            {
                onInit?.Invoke();
            }
            else
            {
                m_onInitialized += onInit;
            }
        }
    }
}
