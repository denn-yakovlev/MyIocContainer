using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IocContainer
{
    public class IocContainer
    {
        private readonly IDictionary<Type, IServiceFactory> _container;

        private IocContainer(IDictionary<Type, IServiceFactory> container)
        {
            _container = container;
        }

        public T Provide<T>() => (T)_container[typeof(T)].GetInstance();

        public static IocContainer Create()
        {
            var container = new Dictionary<Type, IServiceFactory>();

            var assembly = Assembly.GetCallingAssembly();
            var implementationTypes = FindServices(assembly);

            foreach (var implementationType in implementationTypes)
            {
                var publicCtors = GetPublicCtors(implementationType);
                ConstructorInfo primaryCtor;
                try
                {
                    primaryCtor = TryGetPrimaryCtorInfo(publicCtors);
                }
                catch (InvalidOperationException)
                {
                    throw new ConstructorsAmbiguityException(implementationType);
                }

                var ctorParams = primaryCtor.GetParameters();
                var injectedParams = ctorParams
                    .Select(p => InjectParameter(p,container))
                    .Select(Expression.Constant);
                
                var lazyFactory = new Lazy<Func<object>>(() => 
                    Expression.Lambda<Func<object>>(
                            Expression.New(primaryCtor, injectedParams)
                    )
                    .Compile()
                );
                
                var serviceType = ResolveServiceType(implementationType);
                if (container.ContainsKey(serviceType))
                    throw new InvalidOperationException(
                        $"Type {implementationType} is already registered as {serviceType}"
                        );

                var scope = implementationType.GetCustomAttribute<ServiceAttribute>()?.Scope;
                IServiceFactory provider = scope switch
                {
                    Scope.Singleton => new SingletonServiceFactory(lazyFactory),
                    Scope.Transient => new TransientServiceFactory(lazyFactory),
                    _ => throw new ArgumentException()
                };
                
                container[serviceType] = provider;
            }
            return new IocContainer(container);
        }

        private static Type ResolveServiceType(Type implementationType)
        {
            if (Attribute.IsDefined(implementationType, typeof(ProvideAsAttribute)))
                return implementationType
                    .GetCustomAttribute<ProvideAsAttribute>()
                    ?.ServiceType;
            return implementationType;
        }

        private static object InjectParameter(ParameterInfo paramInfo, IDictionary<Type, IServiceFactory> container)
        {
            if (Attribute.IsDefined(paramInfo, typeof(InjectAttribute)))
                return paramInfo.GetCustomAttribute<InjectAttribute>().Injection;

            if (container.ContainsKey(paramInfo.ParameterType))
                return container[paramInfo.ParameterType].GetInstance();
              
            throw new ArgumentException(
                 $"No value to inject found in constructor parameter {paramInfo.Name} " +
                $"of type {paramInfo.Member.DeclaringType}"
            );
        }

        private static IEnumerable<Type> FindServices(Assembly assembly) => 
            assembly
            .GetTypes()
            .Where(type => type.IsClass)
            .Where(cls => cls.GetCustomAttribute<ServiceAttribute>() != null);

        private static ConstructorInfo TryGetPrimaryCtorInfo(IEnumerable<ConstructorInfo> publicCtors) => 
            publicCtors
            .SingleOrDefault(ctorInfo =>
                Attribute.IsDefined(ctorInfo, typeof(PrimaryCtorAttribute))
            ) 
            ?? publicCtors.SingleOrDefault();

        private static IEnumerable<ConstructorInfo> GetPublicCtors(Type serviceType) => 
            serviceType.GetConstructors().Where(ctorInfo => ctorInfo.IsPublic);
    }
}