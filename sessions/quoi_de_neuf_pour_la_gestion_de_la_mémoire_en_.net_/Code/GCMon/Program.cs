using System;
using System.Diagnostics;
using System.Runtime;
using System.Threading;

namespace GCMon
{
    internal class Program
    {
        static ManualResetEvent _stopEvent;

        static void Main(string[] args)
        {
            GCSettings.LatencyMode = GCLatencyMode.Interactive;
            Console.WriteLine($"GCSettings.LatencyMode = {GCSettings.LatencyMode}");
            Console.WriteLine($"pid = {Process.GetCurrentProcess().Id}");

            CreateGCNotification();

            Console.WriteLine("Trigger an induced GC");
            Console.ReadLine();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);

            Console.WriteLine("Allocate to trigger GCs");
            Console.ReadLine();
            _stopEvent = new ManualResetEvent(false);
            var thread = new Thread(Allocate);
            thread.Start();

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
            _stopEvent.Set();
        }

        private static void CreateGCNotification()
        {
            var notif = new GCNotif();
            notif.OnGC += OnGC;
        }

        private static void Allocate(object obj)
        {
            const int LEN = 1_000_000;
            byte[][] list = new byte[LEN][];
            for (int i = 0; i < LEN; ++i)
            {
                list[i] = new byte[25000];
                if (i % 100 == 0)
                {
                    // don't trigger GCs too fast
                    Thread.Sleep(500);

                    if (_stopEvent.WaitOne(1))
                    {
                        Console.WriteLine("Stop allocating...");
                        return;
                    }

                    if (GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2) >= 20)
                    {
                        Console.WriteLine($"Leaving at i = {i}");
                        break;
                    }
                }
            }
        }

        private static void OnGC(object sender, EventArgs e)
        {
            GCMemoryInfo info = GC.GetGCMemoryInfo(GCKind.Any);

            Console.WriteLine();
            var originalForeground = Console.ForegroundColor;
            var originalBackground = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.Write($"#{info.Index} is gen{info.Generation}");
            if (info.Compacted)
            {
                Console.Write(" compacting");
            }
            if (info.Concurrent)
            {
                Console.Write(" concurrent");
            }
            Console.ForegroundColor = originalForeground;
            Console.BackgroundColor = originalBackground;
            Console.WriteLine();

            Console.WriteLine($"High Mem threshold = {info.HighMemoryLoadThresholdBytes}");
            Console.WriteLine($"High Mem load      = {info.MemoryLoadBytes}");
            Console.WriteLine($"Heap Size bytes    = {info.HeapSizeBytes}");
            Console.WriteLine($"Promoted bytes     = {info.PromotedBytes}");
            Console.WriteLine($"Fragmented bytes   = {info.FragmentedBytes}");
            Console.WriteLine($"Committed bytes    = {info.TotalCommittedBytes}");
            Console.WriteLine($"Total Avail. bytes = {info.TotalAvailableMemoryBytes}");
            Console.WriteLine($"Pinned obj count   = {info.PinnedObjectsCount}");
            Console.WriteLine($"Finalization pend  = {info.FinalizationPendingCount}");
            Console.WriteLine($"Pause time %       = {info.PauseTimePercentage}");
            foreach (var pause in info.PauseDurations)
            {
                if (pause.TotalNanoseconds > 0)  // don't show pause-less phases
                {
                    Console.WriteLine($"  {pause.TotalMicroseconds} micro seconds");
                }
            }

            for (int gen = 0; gen < info.GenerationInfo.Length; gen++)
            {
                var genInfo = info.GenerationInfo[gen];
                if (gen == 4)
                {
                    Console.WriteLine($"POH");
                }
                else
                if (gen == 3)
                {
                    Console.WriteLine($"LOH");
                }
                else
                {
                    Console.WriteLine($"Gen {gen}");
                }
                Console.WriteLine($"   Fragmentation before = {genInfo.FragmentationBeforeBytes}");
                Console.WriteLine($"   Fragmentation after  = {genInfo.FragmentationAfterBytes}");
                Console.WriteLine($"            Size before = {genInfo.SizeBeforeBytes}");
                Console.WriteLine($"            Size after  = {genInfo.SizeAfterBytes}");
            }
        }
    }
}
