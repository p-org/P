using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.Pc
{
    public class CompilerFactory
    {
        public CompilerFactory()
        {
            Compose();
        }

        [ImportMany]
        public IEnumerable<Lazy<ICompiler, ICompilerMetadata>> AllCompilers { get; set; }

        private void Compose()
        {
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            IEnumerable<Assembly> assemblies = Directory.GetFiles(directory, "*.dll")
                                                        .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath);

            ContainerConfiguration configuration = new ContainerConfiguration().WithAssemblies(assemblies);
            using (CompositionHost container = configuration.CreateContainer())
            {
                container.SatisfyImports(this);
            }
        }
    }

    public interface ICompilerMetadata
    {
        IEnumerable<string> ProvidedTargets { get; }
    }
}
