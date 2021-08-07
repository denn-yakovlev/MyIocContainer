using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IocContainer
{
    // By default injects into properties marked with [PropertyInjection].
    // Property must have public setter
    class PropertyInjector : MemberInjector
    {
        private IEnumerable<InjectionTarget> _propertyTargets;
        public PropertyInjector(
            Type serviceType, IDictionary<Type, ServiceInfo> container, Expression newInstanceVariable
        ) : base(serviceType, container, newInstanceVariable) 
        {
            
        }

        protected override IEnumerable<InjectionTarget> FilterTargetsToInject()
        {
            _propertyTargets = serviceType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(prop => prop.IsDefined(typeof(PropertyInjectionAttribute)) && prop.CanWrite)
                .Select(prop => new InjectionTarget(prop.PropertyType, prop));
            return _propertyTargets;
        }
        

        protected override Expression BuildInjectionExpression(IEnumerable<Expression> injectedObjectsExpressions)
        {
            var propertyAssignments = _propertyTargets.Zip(
                injectedObjectsExpressions,
                (target, objExpr) => Expression.Assign(
                    Expression.Property(_instanceVariable, (PropertyInfo)target.Member),
                    objExpr
                )
            );
            return Expression.Block(propertyAssignments);
        }
    }
}