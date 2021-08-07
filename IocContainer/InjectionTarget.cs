using System;
using System.Linq;
using System.Reflection;

namespace IocContainer
{
    record InjectionTarget
    {
        public Type Type { get; }
        public ICustomAttributeProvider Member { get; }

        public InjectAttribute InjectAttribute { get; }
        
        public bool ConstantValueCanBeInjected => InjectAttribute != null;
        
        public InjectionTarget(Type type, ICustomAttributeProvider member)
        {
            Type = type;
            Member = member;
            InjectAttribute = (InjectAttribute) Member.GetCustomAttributes(typeof(InjectAttribute), false)
                .SingleOrDefault();
        }
    }
}