namespace Core
{
    using System.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Loader;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyModel;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Implements the <see cref="IAssemblyProvider">IAssemblyProvider</see> interface and represents
    /// default assembly provider that gets assemblies from a specific folder and web application dependencies
    /// with the ability to filter the discovered assemblies with the <c>IsCandidateAssembly</c> and
    /// <c>IsCandidateCompilationLibrary</c> predicates.
    /// </summary>
    public class AssemblyProvider : IAssemblyProvider
    {
        protected ILogger<AssemblyProvider> logger;
        private static List<Assembly> loadedAssemblies = new List<Assembly>();

        /// <summary>
        /// Gets or sets the predicate that is used to filter discovered assemblies from a specific folder.
        /// </summary>
        public Func<Assembly, bool> IsCandidateAssembly { get; set; }

        /// <summary>
        /// Gets or sets the predicate that is used to filter discovered libraries from a web application dependencies.
        /// </summary>
        public Func<Library, bool> IsCandidateCompilationLibrary { get; set; }

        public AssemblyProvider(IServiceProvider serviceProvider)
        {
            logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<AssemblyProvider>();
            IsCandidateAssembly = assembly =>
              !assembly.FullName.StartsWith("Microsoft.") && !assembly.FullName.StartsWith("System.") && !assembly.CodeBase.Contains("dotnet-razor-tooling");
            IsCandidateCompilationLibrary = library =>
                library.Name != "NETStandard.Library" && 
                !library.Name.ToLower().StartsWith("microsoft.") && 
                !library.Name.ToLower().StartsWith("system.") &&
                !library.Name.ToLower().StartsWith("aspnet");
        }

        /// <summary>
        /// Discovers and then gets the discovered assemblies from a specific folder and web application dependencies.
        /// </summary>
        /// <param name="path">The extensions path of a web application.</param>
        /// <returns></returns>
        public IEnumerable<Assembly> GetAssemblies(string path)
        {
            if (loadedAssemblies.Any())
            {
                return loadedAssemblies;
            }

            var assemblies = new List<Assembly>();
            assemblies.AddRange(GetAssembliesFromPath(path));
            assemblies.AddRange(GetAssembliesFromDependencyContext());

            assemblies = assemblies.Distinct().ToList();
            loadedAssemblies = assemblies;
            return assemblies;
        }

        private IEnumerable<Assembly> GetAssembliesFromPath(string path)
        {
            List<Assembly> assemblies = new List<Assembly>();

            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                logger.LogInformation("Discovering and loading assemblies from path '{0}'", path);

                var directories = Directory.GetDirectories(path);
                foreach (var directory in directories)
                {
                    foreach (string extensionPath in Directory.EnumerateFiles(directory, "*.dll"))
                    {
                        Assembly assembly = null;

                        try
                        {
                            assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(extensionPath);

                            if (IsCandidateAssembly(assembly))
                            {
                                assemblies.Add(assembly);
                                logger.LogInformation("Assembly '{0}' is discovered and loaded", assembly.FullName);
                            }
                        }

                        catch (Exception e)
                        {
                            logger.LogWarning("Error loading assembly '{0}'", extensionPath);
                            logger.LogInformation(e.ToString());
                        }
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(path))
                {
                    logger.LogWarning("Discovering and loading assemblies from path skipped: path not provided", path);
                }
                else
                {
                    logger.LogWarning("Discovering and loading assemblies from path '{0}' skipped: path not found", path);
                }
            }

            return assemblies;
        }

        private IEnumerable<Assembly> GetAssembliesFromDependencyContext()
        {
            List<Assembly> assemblies = new List<Assembly>();

            logger.LogInformation("Discovering and loading assemblies from DependencyContext");

            foreach (CompilationLibrary compilationLibrary in DependencyContext.Default.CompileLibraries)
            {
                if (IsCandidateCompilationLibrary(compilationLibrary))
                {
                    Assembly assembly = null;

                    try
                    {
                        assembly = Assembly.Load(new AssemblyName(compilationLibrary.Name));
                        assemblies.Add(assembly);
                        logger.LogInformation("Assembly '{0}' is discovered and loaded", assembly.FullName);
                    }

                    catch (Exception e)
                    {
                        logger.LogWarning("Error loading assembly '{0}'", compilationLibrary.Name);
                        logger.LogInformation(e.ToString());
                    }
                }
            }

            return assemblies;
        }
    }
}