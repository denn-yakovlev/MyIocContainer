using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IocContainer
{
    abstract class Injector
    {
        protected readonly Type serviceType;
        protected readonly IDictionary<Type, IServiceFactory> _container;

        protected Injector(Type serviceType, IDictionary<Type, IServiceFactory> container)
        {
            this.serviceType = serviceType;
            _container = container;
        }

        public Expression GetInjectionExpression()
        {
            var membersToInject = FilterTargetsToInject();
            var memberInjectionExpressions = membersToInject.Select(
                member => GetInjectedObjectExpression(member)
            );
            return BuildInjectionExpression(memberInjectionExpressions);
        }

        protected abstract IEnumerable<InjectionTarget> FilterTargetsToInject();
        protected abstract Expression BuildInjectionExpression(IEnumerable<Expression> injectedObjectsExpressions);

        private Expression GetInjectedObjectExpression(InjectionTarget target)
        {
            return Expression.Convert(
                Expression.Call(
                    Expression.Constant(this),
                    typeof(Injector).GetMethod(nameof(Inject), BindingFlags.Instance | BindingFlags.NonPublic),
                    Expression.Constant(target)
                ),
                target.Type
            );
        }

        private object Inject(InjectionTarget injectionTarget)
        {
            if (injectionTarget.ConstantValueCanBeInjected)
                return injectionTarget.InjectAttribute.Value;
            bool shouldInjectServiceFromContainer = _container.ContainsKey(injectionTarget.Type);
            if (shouldInjectServiceFromContainer)
                return _container[injectionTarget.Type].GetInstance();
            throw new ArgumentException($"No value to inject found of type {injectionTarget.Type}");
        }
    }
}