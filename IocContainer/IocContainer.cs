using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
                var constructorInjectionExpr = new ConstructorInjector(implementationType, container)
                    .GetInjectionExpression();

                var serviceInstanceFactory = Lambda<Func<object>>(
                    constructorInjectionExpr
                ).Compile();

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

        private static Type ResolveServiceType(Type implementationType)
        {
            bool implTypeHasProvideAsAttribute = Attribute.IsDefined(
                implementationType, typeof(ProvideAsAttribute)
            );
            if (implTypeHasProvideAsAttribute)
                return implementationType.GetCustomAttribute<ProvideAsAttribute>()?.ServiceType;
            return implementationType;
            // new Injector().GetInjectionExpression((MethodInfo methodInfo) => true);
        }

        private static IEnumerable<Type> FindServices(Assembly assembly) =>
            assembly
                .GetTypes()
                .Where(type => type.IsClass)
                .Where(cls => cls.GetCustomAttribute<ServiceAttribute>() != null);

    }

    class ConstructorInjector 
    {
        private readonly ConstructorInfo _primaryCtor;
        private readonly IDictionary<Type, IServiceFactory> _container;
        
        public ConstructorInjector(Type type, IDictionary<Type, IServiceFactory> container)
        {
            _container = container;
            var publicCtors = GetPublicCtors(type);
            try
            {
                _primaryCtor = GetPrimaryConstructorInfo(publicCtors);
            }
            catch (InvalidOperationException)
            {
                throw new ConstructorsAmbiguityException(type);
            }
        }
        
        public Expression GetInjectionExpression()
        {

            var paramsInjectionExpression = _primaryCtor
                .GetParameters()
                .Select(param => InjectParameterExpression(param));
            return New(_primaryCtor, paramsInjectionExpression);
        }

        private Expression InjectParameterExpression(ParameterInfo param) => 
            Convert(
                Call(
                    Constant(this),
                    GetType().GetMethod(nameof(InjectParameter), BindingFlags.Instance | BindingFlags.NonPublic),
                    Constant(param)
                ),
                param.ParameterType
            );
        
        private object InjectParameter(ParameterInfo paramInfo)
        {
            bool shouldInjectConstantValue = Attribute.IsDefined(paramInfo, typeof(InjectAttribute));
            if (shouldInjectConstantValue)
                return paramInfo.GetCustomAttribute<InjectAttribute>().Value;
            
            bool shouldInjectServiceFromContainer = _container.ContainsKey(paramInfo.ParameterType);
            if (shouldInjectServiceFromContainer)
                return _container[paramInfo.ParameterType].GetInstance();

            throw new ArgumentException(
                $"No value to inject found in constructor parameter {paramInfo.Name} " +
                $"of type {paramInfo.ParameterType} in {paramInfo.Member.DeclaringType}"
            );
        }

        private static bool ConstructorIsPrimary(ConstructorInfo ctorInfo) =>
            Attribute.IsDefined(ctorInfo, typeof(PrimaryCtorAttribute));

        private static ConstructorInfo GetPrimaryConstructorInfo(IEnumerable<ConstructorInfo> publicCtors) =>
            publicCtors.SingleOrDefault(ctorInfo => ConstructorIsPrimary(ctorInfo)) ?? publicCtors.SingleOrDefault();

        private static IEnumerable<ConstructorInfo> GetPublicCtors(Type serviceType) =>
            serviceType.GetConstructors().Where(ctorInfo => ctorInfo.IsPublic);
    }
}