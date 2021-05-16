using System;

namespace IocContainer
{
    public class ProvideAsAttribute : Attribute
    {
        public Type ServiceType { get; }
        
        public ProvideAsAttribute(Type serviceType)
        {
            ServiceType = serviceType;
        }
    }
}