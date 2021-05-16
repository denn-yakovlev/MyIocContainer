using System;
using System.Linq.Expressions;

namespace IocContainer
{
    class SingletonServiceFactory : IServiceFactory
    {
        private readonly Lazy<object> _lazyInstance;

        public SingletonServiceFactory(Lazy<Func<object>> lazyFactory)
        {
            _lazyInstance = new Lazy<object>(() => lazyFactory.Value.Invoke());
        }

        public object GetInstance() => _lazyInstance.Value;
    }
}