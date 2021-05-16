using System;

namespace IocContainer.Demo
{
    [Service(Scope.Singleton)]
    class A
    {
        private int _a;

        [PrimaryCtor]
        public A([Inject(10)] int a)
        {
            _a = a;
        }

        public A(string someStr)
        {
        }

        public override string ToString() => $"A({_a})";
    }

    [Service(Scope.Singleton)]
    class B
    {
        public override string ToString() => "B";
    }

    [Service(Scope.Transient)]
    class C
    {
        private A _a;
        private int _num;
        private string _str;

        public C(A a, [Inject(100500)] int num, [Inject("kek")] string str)
        {
            _a = a;
            _num = num;
            _str = str;
        }

        public override string ToString() =>
            $"{_a}, {_num}, {_str}";
    }

    interface ISomeInterface
    {
            
    }

    class D : ISomeInterface
    {
            
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            var container = IocContainer.Create();
            var a = container.Provide<A>();
            var b = container.Provide<B>();
            var c = container.Provide<C>();
            Console.WriteLine(a);
            Console.WriteLine(b);
            Console.WriteLine(c);

            var a1 = container.Provide<A>();
            Console.WriteLine($"a == a1 ? -{ReferenceEquals(a, a1)}");

            var b1 = container.Provide<B>();
            Console.WriteLine($"b == b1 ? -{ReferenceEquals(b, b1)}");

            var c1 = container.Provide<C>();
            Console.WriteLine($"c == c1 ? -{ReferenceEquals(c, c1)}");

            // Throws KeyNotFound exception
            //var d = container.Provide<ISomeInterface>();
        }
    }
}