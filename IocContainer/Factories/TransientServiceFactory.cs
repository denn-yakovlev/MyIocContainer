using System;
using System.Linq.Expressions;

namespace IocContainer
{
    class TransientServiceFactory : IServiceFactory
    {
        private readonly Lazy<Func<object>> _lazyFactory;

        public TransientServiceFactory(Lazy<Func<object>> lazyFactory)
        {
            _lazyFactory = lazyFactory;
        }

        public object GetInstance() =>
            _lazyFactory.Value.Invoke();
    }
}