using System.Runtime.InteropServices;
using System.Xml.Schema;

namespace DatasTest
{
    internal class Program
    {
        const int Delay = 50;

        const int Seed = 1337;

        static int BucketsCount = 128;

        static List<object>[] Buckets = new List<object>[BucketsCount];

        static int MaxThreads = 8;

        static int Index = 0;

        static (string name, Action<int> action)[] Scenarios =
        {
            (nameof(Idle), Idle),
            (nameof(RushHour), RushHour),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),
            (nameof(Idle), Idle),

            //(nameof(ReloadCaches), ReloadCaches)
        };


        [StructLayout(LayoutKind.Explicit, Size = 1024)]
        public struct Payload
        {

        }

        public class Reference
        {
            public Payload Payload;
        }


        static void Main(string[] args)
        {
            Console.WriteLine($"Pid: {Environment.ProcessId}");
            if (args.Length > 0)
            {
                System.Diagnostics.Process.Start(args[0], Environment.ProcessId.ToString());
            }
            Console.ReadLine();


            foreach (var (key, value) in GC.GetConfigurationVariables())
            {
                Console.WriteLine($"{key}: {value}");
            }

            for (int i = 0; i < BucketsCount; i++)
            {
                Buckets[i] = new();
            }

            GC.Collect(2);

            Console.WriteLine("Ready");
            //Thread.Sleep(3000);

            if (Console.ReadLine() == "q")
            {
                return;
            }

            Task.Run(Process);

            while (true)
            {
                if (Console.ReadLine() == "q")
                {
                    break;
                }

            }
        }

        static void Process()
        {
            var rng = new Random(Seed);

            int count = 0;

            while (true)
            {
                foreach (var scenario in Scenarios)
                {
                    var seed = rng.Next();
                    Console.WriteLine($"Cycle {++count}, playing scenario {scenario.name}");
                    scenario.action(seed);
                }

                //var index = rng.Next(0, Scenarios.Length);
                //var scenario = Scenarios[index];
                //var seed = rng.Next();

                //Console.WriteLine($"Cycle {++count}, playing scenario {scenario.name}");

                //scenario.action(seed);
            }

            //while (true)
            //{
            //    var nbThreads = rng.Next(1, MaxThreads);

            //    if (nbThreads == MaxThreads - 1)
            //    {
            //        nbThreads = 32;
            //    }

            //    var tasks = new Task[nbThreads];

            //    for (int i = 0; i < nbThreads; i++)
            //    {
            //        var seed = rng.Next();
            //        var iterations = rng.Next(10, 50);

            //        tasks[i] = Task.Run(() => BackgroundThread(seed, iterations));
            //    }

            //    Console.WriteLine($"Cycle {++count}, {nbThreads} threads");

            //    Task.WaitAll(tasks);
            //    Thread.Sleep(Delay);
            //}
        }

        static void Idle(int seed)
        {
            var rng = new Random(seed);
            var tasks = new Task[1];

            for (int i = 0; i < tasks.Length; i++)
            {
                var threadSeed = rng.Next();
                var iterations = rng.Next(20, 50);

                tasks[i] = Task.Run(() => BackgroundThread(threadSeed, iterations, 50, 1));
            }

            Task.WaitAll(tasks);
            Thread.Sleep(1000);
        }

        static void RushHour(int seed)
        {
            var rng = new Random(seed);
            var tasks = new Task[64];

            for (int i = 0; i < tasks.Length; i++)
            {
                var threadSeed = rng.Next();
                var iterations = rng.Next(50, 100);

                tasks[i] = Task.Run(() => BackgroundThread(threadSeed, iterations, 250, 1));
            }

            Task.WaitAll(tasks);
        }

        static void ReloadCaches(int seed)
        {

        }

        static void BackgroundThread(int seed, int iterations, int delay, int collectionsPerCycle)
        {
            var rng = new Random(seed);

            for (int i = 0; i < iterations; i++)
            {
                OneCycle(rng, collectionsPerCycle);
                Thread.Sleep(delay);
            }
        }

        static void OneCycle(Random rng, int collectionsPerCycle)
        {
            var allocations = rng.Next(1, 1000);
            var bucket = rng.Next(0, BucketsCount);

            lock (Buckets[bucket])
            {
                for (int i = 0; i < allocations; i++)
                {
                    Buckets[bucket].Add(new Reference());
                }
            }

            var lohAllocations = rng.Next(0, 4);
            var lohBucket = rng.Next(0, BucketsCount);

            lock (Buckets[lohBucket])
            {
                for (int i = 0; i < lohAllocations; i++)
                {
                    var size = rng.Next(85, 250);

                    Buckets[lohBucket].Add(new Payload[size]);
                }
            }

            for (int i = 0; i < collectionsPerCycle; i++)
            {
                var bucketToFree = rng.Next(0, BucketsCount);

                lock (Buckets[bucketToFree])
                {
                    Buckets[bucketToFree].Clear();
                }
            }
        }
    }
}
