namespace Core
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A class that finds types needed by StoreEye by looping assemblies in the 
    /// currently executing AppDomain. Only assemblies whose names matches
    /// certain patterns are investigated and an optional list of assemblies
    /// referenced by <see cref="ConfigredAssemblies"/> are always investigated.
    /// </summary>
    public class TypeFinder : ITypeFinder
    {
        private readonly ILogger<TypeFinder> _logger;
        private readonly IAssemblyProvider _assemblyProvider;

        public TypeFinder(ILogger<TypeFinder> logger, IAssemblyProvider assemblyProvider)
        {
            _logger = logger;
            _assemblyProvider = assemblyProvider;
        }

        public IEnumerable<Type> FindClassesOfType<T>(bool onlyConcreteClasses = true)
        {
            return FindClassesOfType(typeof(T), onlyConcreteClasses);
        }

        public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses = true)
        {
            return FindClassesOfType(assignTypeFrom, _assemblyProvider.GetAssemblies(AppContext.BaseDirectory), onlyConcreteClasses);
        }

        public IEnumerable<Type> FindClassesOfType<T>(IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true)
        {
            return FindClassesOfType(typeof(T), assemblies, onlyConcreteClasses);
        }

        public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies, bool onlyConcreteClasses = true)
        {
            var result = new List<Type>();
            try
            {
                foreach (var assembly in assemblies)
                {
                    Type[] types = assembly.GetTypes();

                    foreach (var type in types)
                    {
                        if (assignTypeFrom.IsAssignableFrom(type) || (assignTypeFrom.GetTypeInfo().IsGenericTypeDefinition && DoesTypeImplementOpenGeneric(type, assignTypeFrom)))
                        {
                            if (!type.GetTypeInfo().IsInterface)
                            {
                                if (onlyConcreteClasses)
                                {
                                    if (type.GetTypeInfo().IsClass && !type.GetTypeInfo().IsAbstract)
                                    {
                                        result.Add(type);
                                    }
                                }
                                else
                                {
                                    result.Add(type);
                                }
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var message = string.Empty;

                foreach (var e in ex.LoaderExceptions)
                {
                    message += e.Message + Environment.NewLine;
                }

                var exception = new Exception(message, ex);

                throw exception;
            }

            return result;
        }
        
        /// <summary>
        /// Does type implement generic?
        /// </summary>
        /// <param name="type"></param>
        /// <param name="openGeneric"></param>
        /// <returns></returns>
        protected virtual bool DoesTypeImplementOpenGeneric(Type type, Type openGeneric)
        {
            try
            {
                var genericTypeDefinition = openGeneric.GetGenericTypeDefinition();
                
                foreach (var implementedInterface in type.GetTypeInfo().FindInterfaces((objType, objCriteria) => true, null))
                {
                    if (!implementedInterface.GetTypeInfo().IsGenericType)
                        continue;

                    var isMatch = genericTypeDefinition.IsAssignableFrom(implementedInterface.GetGenericTypeDefinition());

                    return isMatch;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
