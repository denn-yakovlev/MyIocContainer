using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IocContainer
{
    abstract class MemberInjector : Injector
    {
        protected Expression _instanceVariable;

        protected MemberInjector(
            Type serviceType, IDictionary<Type, IServiceFactory> container, Expression instanceVariable
        ) : base(serviceType, container)
        {
            _instanceVariable = instanceVariable;
        }
    }
}