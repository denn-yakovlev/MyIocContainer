using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static System.Linq.Expressions.Expression;

namespace IocContainer
{
    public class IocContainer
    {
        private readonly IDictionary<Type, IServiceFactory> _container;

        private IocContainer(IDictionary<Type, IServiceFactory> container)
        {
            _container = container;
        }

        public T Provide<T>() => (T) _container[typeof(T)].GetInstance();

        public static IocContainer Create()
        {
            var container = new Dictionary<Type, IServiceFactory>();

            var assembly = Assembly.GetCallingAssembly();
            var implementationTypes = FindServices(assembly);

            foreach (var implementationType in implementationTypes)
            {
                var serviceInstanceFactory = GetServiceInstanceFactory(implementationType, container);

                var serviceType = ResolveServiceType(implementationType);
                if (container.ContainsKey(serviceType))
                    throw new InvalidOperationException(
                        $"Type {implementationType} is already registered as {serviceType}"
                    );

                var scope = implementationType.GetCustomAttribute<ServiceAttribute>()?.Scope;
                IServiceFactory provider = scope switch
                {
                    Scope.Singleton => new SingletonServiceFactory(serviceInstanceFactory),
                    Scope.Transient => new TransientServiceFactory(serviceInstanceFactory),
                    _ => throw new ArgumentException()
                };

                container[serviceType] = provider;
            }

            return new IocContainer(container);
        }

        private static Func<object> GetServiceInstanceFactory(Type implementationType, Dictionary<Type, IServiceFactory> container)
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

        private static Type ResolveServiceType(Type implementationType)
        {
            bool implTypeHasProvideAsAttribute = Attribute.IsDefined(
                implementationType, typeof(ProvideAsAttribute)
            );
            if (implTypeHasProvideAsAttribute)
                return implementationType.GetCustomAttribute<ProvideAsAttribute>()?.ServiceType;
            return implementationType;
        }

        private static IEnumerable<Type> FindServices(Assembly assembly) =>
            assembly
                .GetTypes()
                .Where(type => type.IsClass)
                .Where(cls => cls.GetCustomAttribute<ServiceAttribute>() != null);

    }
}