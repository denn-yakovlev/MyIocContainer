using System;

namespace IocContainer
{
    class TransientServiceFactory : IServiceFactory
    {
        private readonly Func<object> _factory;

        public TransientServiceFactory(Func<object> factory) =>
            _factory = factory;

        public object GetInstance() =>
            _factory();
    }
}