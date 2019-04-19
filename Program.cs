using System;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Unit = System.ValueTuple;

namespace STM
{
    using static Operations;
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }

        private static STML<Unit> IncrementCounter(TVar<int> counter) =>
            from transaction in ModifyTVar<int>(0)
            select Unit();
    }
}
