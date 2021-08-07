using System;
using System.Linq.Expressions;

namespace IocContainer
{
    class TransientServiceFactory : IServiceFactory
    {
        private readonly Func<object> _instanceFactory;

        public TransientServiceFactory(Func<object> instanceFactory)
        {
            _instanceFactory = instanceFactory;
        }

        public object GetInstance() => _instanceFactory?.Invoke();
    }
}