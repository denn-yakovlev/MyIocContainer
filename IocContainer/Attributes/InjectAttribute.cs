using System;

namespace IocContainer
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class InjectAttribute : Attribute
    {
        public object Injection { get; }

        public InjectAttribute(object injection)
        {
            Injection = injection;
        }
    }
}