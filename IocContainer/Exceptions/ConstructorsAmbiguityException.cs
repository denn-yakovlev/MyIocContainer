using System;

namespace IocContainer
{
    class ConstructorsAmbiguityException : Exception
    {
        public override string Message => $"Class '{_targetClass}' has more than 1 public constructor " +
                                          $"and neither is marked with {nameof(PrimaryCtorAttribute)}, " +
                                          $"or there are more than 1 primary constructors";

        private Type _targetClass;

        public ConstructorsAmbiguityException(Type targetClass)
        {
            _targetClass = targetClass;
        }
    }
}