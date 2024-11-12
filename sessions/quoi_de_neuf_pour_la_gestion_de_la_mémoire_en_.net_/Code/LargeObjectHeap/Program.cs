using System;

namespace LargeObjectHeap
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // interview question: in which generation will this string be allocated?
            //
            string largeString = new string('*', 42501);
            Console.ReadKey();
            Console.WriteLine($"in gen{GC.GetGeneration(largeString)}");
        }
    }
}
