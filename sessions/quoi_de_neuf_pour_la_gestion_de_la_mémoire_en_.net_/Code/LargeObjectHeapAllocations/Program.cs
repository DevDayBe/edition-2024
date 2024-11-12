using System;
using System.Diagnostics;
using System.Threading;

namespace LargeObjectHeapAllocations
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var watch = new Stopwatch();
            watch.Start();

            Allocate(100000);

            watch.Stop();
            Console.WriteLine($"{watch.ElapsedMilliseconds} ms");
            Console.WriteLine($"   gen0 | {GC.CollectionCount(0)}");
            Console.WriteLine($"   gen1 | {GC.CollectionCount(1)}");
            Console.WriteLine($"   gen2 | {GC.CollectionCount(2)}");
            //
            // default 85000
            // --------------
            // 1020 ms
            //    gen0 | 2702
            //    gen1 | 2702
            //    gen2 | 2702
            //
            //
            // 100 KB
            // ---------------
            // 424 ms
            //    gen0 | 1010
            //    gen1 | 1
            //    gen2 | 0

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }

        private static void Allocate(int count)
        {
            byte[] buffer = null;

            for (int i = 0; i < count; i++)
            {
                buffer = new byte[85000];
                buffer[0] = (byte)i;
            }
        }
    }
}
