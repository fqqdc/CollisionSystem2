using System.Diagnostics;

namespace Test
{
    struct S
    {
        public int Value { get; set; }
        public int Value2 { get; set; } = -1;
        public int Value3 { get; set; } = -1;

        public S(int value) { Value = 10; }
        public S() { Value = 10; }
    }

    

    internal class Program
    {
        static void Main(string[] args)
        {
            S s = new S(10);

            Console.WriteLine(s.Value);
            Console.WriteLine(s.Value2);
            Console.WriteLine(s.Value3);
        }
    }
}