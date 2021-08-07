using System;
using System.Linq.Expressions;

namespace IocContainer
{
    class SingletonServiceFactory : IServiceFactory
    {
        private readonly Lazy<object> _lazyInstance;

        public SingletonServiceFactory(Func<object> instanceFactory)
        {
            _lazyInstance = new Lazy<object>(() => instanceFactory?.Invoke());
        }

        public object GetInstance() => _lazyInstance.Value;
    }
}