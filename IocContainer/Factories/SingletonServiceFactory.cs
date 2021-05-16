using System;

namespace IocContainer
{
    class SingletonServiceFactory : IServiceFactory
    {
        private readonly Lazy<object> _lazyInstance;

        public SingletonServiceFactory(Func<object> factory) =>
            _lazyInstance = new Lazy<object>(factory, true);

        public object GetInstance() =>
            _lazyInstance.Value;
    }
}