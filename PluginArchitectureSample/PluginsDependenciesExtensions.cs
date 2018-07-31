namespace PluginArchitectureSample
{
    using System;
    using System.IO;
    using System.Reflection;
    using Core;
    using Microsoft.Extensions.DependencyInjection;
    
    public static class PluginsDependenciesExtensions
    {
        public static void AddDependencies(this IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var assemblyProvider = (IAssemblyProvider)serviceProvider.GetService(typeof(IAssemblyProvider));
            var typeFinder = (ITypeFinder)serviceProvider.GetService(typeof(ITypeFinder));
            
            var path = Directory.GetCurrentDirectory();
            path = Path.Combine(path, "Plugins");
            var assemblies = assemblyProvider.GetAssemblies(path);
            var drTypes = typeFinder.FindClassesOfType<IDependencyRegister>(assemblies);
            
            foreach (var drType in drTypes)
            {
                if (typeof(IControllersContainer).IsAssignableFrom(drType))
                {
                    // http://stackoverflow.com/questions/37725934/asp-net-core-mvc-controllers-in-separate-assembly
                    services.AddMvcCore().AddApplicationPart(drType.GetTypeInfo().Assembly);
                }

                var drInstance = (IDependencyRegister)Activator.CreateInstance(drType);
                drInstance.Register(services);
            }
        }
    }
}
