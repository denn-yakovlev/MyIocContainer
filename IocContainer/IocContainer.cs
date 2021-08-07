using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static System.Linq.Expressions.Expression;

namespace IocContainer
{
    interface IServiceContainer
    {
        
    }
    
    public class IocContainer
    {
        private readonly IDictionary<Type, ServiceInfo> _container;

        private IocContainer(IDictionary<Type, ServiceInfo> container)
        {
            _container = container;
        }

        public T Provide<T>() => (T) _container[typeof(T)].Factory.GetInstance();

        public static IocContainer Create()
        {
            var container = new Dictionary<Type, ServiceInfo>();

            var assembly = Assembly.GetCallingAssembly();
            var implementationTypes = FindServices(assembly);

            foreach (var implementationType in implementationTypes)
            {
                var serviceInfo = new ServiceInfo(implementationType);
                var serviceInstanceFactory = GetServiceInstanceFactory(implementationType, container);

                serviceInfo.SetFactory(serviceInstanceFactory);
                
                if (container.ContainsKey(serviceInfo.ServiceType))
                    throw new InvalidOperationException(
                        $"Type {implementationType} is already registered as {serviceInfo.ServiceType}"
                    );

                container[serviceInfo.ServiceType] = serviceInfo;
            }

            return new IocContainer(container);
        }

        private static Func<object> GetServiceInstanceFactory(Type implementationType, Dictionary<Type, ServiceInfo> container)
        {
            var constructorInjectionExpr = new ConstructorInjector(implementationType, container)
                .GetInjectionExpression();
            var newInstanceVariable = Variable(implementationType);
            var fieldInjectionExpr = new FieldInjector(implementationType, container, newInstanceVariable)
                .GetInjectionExpression();
            var propertyInjectionExpr = new PropertyInjector(implementationType, container, newInstanceVariable)
                .GetInjectionExpression();

            /* Compiled body looks like:
             * 
             * <implementationType> $var1 = new <implementationType>(...);
             * $var1.<field1> = ...;
             * $var1.<field2> = ...;
             * ...
             * $var1.<fieldM> = ...;
             * $var1.<prop1> = ...;
             * $var1.<prop2> = ...;
             * ...
             * $var1.<propN> = ...;
             * return $var1 as object;
             */
            var serviceInstanceFactoryBody = Block(
                typeof(object),
                new[] {newInstanceVariable},
                Assign(newInstanceVariable, constructorInjectionExpr),
                fieldInjectionExpr,
                propertyInjectionExpr,
                newInstanceVariable
            );
            var serviceInstanceFactory = Lambda<Func<object>>(serviceInstanceFactoryBody).Compile();
            return serviceInstanceFactory;
        }

        private static IEnumerable<Type> FindServices(Assembly assembly) =>
            assembly
                .GetTypes()
                .Where(type => type.IsClass)
                .Where(cls => cls.GetCustomAttribute<ServiceAttribute>() != null);

    }

    record ServiceInfo
    {
        public Type ImplementationType { get; }
        public Type ServiceType { get; }
        public Scope ServiceScope { get; }
        
        public IServiceFactory Factory { get; private set; }

        public ServiceInfo(Type implementationType)
        {
            ImplementationType = implementationType;
            var provideAsAttr = implementationType.GetCustomAttribute<ProvideAsAttribute>();
            ServiceType = provideAsAttr?.ServiceType ?? implementationType;
            var serviceAttr = implementationType.GetCustomAttribute<ServiceAttribute>();
            ServiceScope = serviceAttr.Scope;
        }

        public void SetFactory(Func<object> factory)
        {
            Factory = ServiceScope switch
            {
                Scope.Singleton => new SingletonServiceFactory(factory),
                Scope.Transient => new TransientServiceFactory(factory),
                _ => throw new ArgumentException()
            };
        }
    }
}