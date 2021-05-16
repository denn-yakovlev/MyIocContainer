using System;

namespace IocContainer
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute
    {
        public Scope Scope { get; }

        public ServiceAttribute(Scope scope)
        {
            Scope = scope;
        }
    }
}