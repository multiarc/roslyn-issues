using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.PlatformAbstractions;

namespace SelfReference
{
    internal static class AssemblyHelper
    {
        private static volatile DependencyContext _dependencyContext;

        private static readonly ConcurrentDictionary<string, Tuple<Assembly, MetadataReference>> AssemblyCache =
            new ConcurrentDictionary<string, Tuple<Assembly, MetadataReference>>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<MetadataReference> MetadataReferences;

        private static void WalkReferenceAssemblies(Assembly current)
        {
            var currentInfo = new Tuple<Assembly, MetadataReference>(current, CreateMetadataFileReference(current));
            if (AssemblyCache.TryAdd(current.FullName, currentInfo))
            {
                MetadataReferences.Add(currentInfo.Item2);
                foreach (var assemblyName in current.GetReferencedAssemblies())
                {
                    try
                    {
                        var dependent = Assembly.Load(assemblyName);
                        var dependentInfo = new Tuple<Assembly, MetadataReference>(dependent, CreateMetadataFileReference(dependent));
                        if (AssemblyCache.TryAdd(dependent.FullName, dependentInfo))
                        {
                            MetadataReferences.Add(dependentInfo.Item2);
                            WalkReferenceAssemblies(dependent);
                        }
                    }
                    catch (FileNotFoundException)
                    {
                    }
                }
            }
        }

        static AssemblyHelper()
        {
            var applicationAssembly = Assembly.Load(new AssemblyName(PlatformServices.Default.Application.ApplicationName));
            _dependencyContext = DependencyContext.Load(applicationAssembly);
            MetadataReferences = new List<MetadataReference>();
            WalkReferenceAssemblies(applicationAssembly);
            GetApplicationReferences();
        }

        internal static Stream GetResourceStream(Assembly assembly, string name)
        {
            return assembly.GetManifestResourceStream(name);
        }

        private static MetadataReference CreateMetadataFileReference(Assembly asm)
        {
            var moduleMetadata = ModuleMetadata.CreateFromFile(asm.Location);
            var metadata = AssemblyMetadata.Create(moduleMetadata);
            return metadata.GetReference(filePath: asm.FullName);
        }

        public static List<MetadataReference> GetApplicationReferences()
        {
            if (_dependencyContext != null)
            {
                foreach (var name in _dependencyContext.GetDefaultAssemblyNames())
                {
                    if (!AssemblyCache.ContainsKey(name.FullName))
                    {
                        var asm = Assembly.Load(name);
                        WalkReferenceAssemblies(asm);
                    }
                }
            }
            return MetadataReferences;
        }

    }
}