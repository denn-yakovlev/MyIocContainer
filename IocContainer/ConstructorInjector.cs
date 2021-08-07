using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IocContainer
{
    // By default injects into single public constructor or public constructor marked with [PrimaryCtor].
    // Throws if cannot inject
    class ConstructorInjector : Injector
    {
        private ConstructorInfo _primaryCtor;
        private readonly IDictionary<Type, IServiceFactory> _container;
        
        public ConstructorInjector(Type serviceType, IDictionary<Type, IServiceFactory> container) : 
            base(serviceType, container)
        {
        }

        protected override IEnumerable<InjectionTarget> FilterTargetsToInject()
        {
            var publicCtors = GetPublicCtors(serviceType);
            try
            {
                _primaryCtor = GetPrimaryConstructorInfo(publicCtors);
            }
            catch (InvalidOperationException)
            {
                throw new ConstructorsAmbiguityException(serviceType);
            }
            return _primaryCtor.GetParameters().Select(ctorParam => 
                new InjectionTarget(ctorParam.ParameterType, ctorParam)
            );
        }

        protected override Expression BuildInjectionExpression(IEnumerable<Expression> injectedObjectsExpressions) {
            return Expression.New(_primaryCtor, injectedObjectsExpressions);
        }
        
        private static ConstructorInfo GetPrimaryConstructorInfo(IEnumerable<ConstructorInfo> publicCtors) =>
            publicCtors.SingleOrDefault(ctorInfo => ConstructorIsPrimary(ctorInfo)) ?? publicCtors.SingleOrDefault();

        private static IEnumerable<ConstructorInfo> GetPublicCtors(Type serviceType) =>
            serviceType.GetConstructors().Where(ctorInfo => ctorInfo.IsPublic);
        
        private static bool ConstructorIsPrimary(ConstructorInfo ctorInfo) =>
            Attribute.IsDefined(ctorInfo, typeof(PrimaryCtorAttribute));
    }
}