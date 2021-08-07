using System;

namespace IocContainer
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
    public class InjectAttribute : Attribute
    {
        public object Value { get; }

        public InjectAttribute(object value)
        {
            Value = value;
        }
    }
}