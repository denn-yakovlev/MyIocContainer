using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IocContainer
{
    // By default injects into fields marked with [FieldInjection]
    // Field must not be readonly
    class FieldInjector : MemberInjector
    {
        private IEnumerable<InjectionTarget> _fieldTargets;

        public FieldInjector(
            Type serviceType, IDictionary<Type, ServiceInfo> container, Expression instanceVariable
        ) : base(serviceType, container, instanceVariable) 
        {
            
        }

        protected override IEnumerable<InjectionTarget> FilterTargetsToInject()
        {
            _fieldTargets = serviceType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.IsDefined(typeof(FieldInjectionAttribute)) && !field.IsInitOnly)
                .Select(field => new InjectionTarget(field.FieldType, field));
            return _fieldTargets;
        }
        

        protected override Expression BuildInjectionExpression(IEnumerable<Expression> injectedObjectsExpressions)
        {
            var fieldAssignments = _fieldTargets.Zip(
                injectedObjectsExpressions,
                (target, objExpr) => Expression.Assign(
                    Expression.Field(_instanceVariable, (FieldInfo)target.Member),
                    objExpr
                )
            );
            return Expression.Block(fieldAssignments);
        }
    }
}