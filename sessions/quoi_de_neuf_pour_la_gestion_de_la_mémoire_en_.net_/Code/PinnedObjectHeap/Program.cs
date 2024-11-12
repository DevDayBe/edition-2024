using Microsoft.Diagnostics.Runtime;
using System;
using System.Diagnostics;
using System.Linq;

namespace PinnedObjectHeap
{
    class ClassWithoutReferenceField
    {
        public int X;
        public int Y;
    }

    struct StructWithReferenceField
    {
        public string S;
        public int Y;
    }

    struct StructWithoutReferenceField
    {
        public int X;
        public int Y;

        // this won't work
        //public StructWithReferenceField S;
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // allocate arrays directly in POH
            //
            byte[] pinned = GC.AllocateArray<byte>(90000, true);  // larger than LOH threshold
            Console.WriteLine("Where is the large pinned array allocated?");
            Console.ReadLine();
            Console.WriteLine($"generation = {GC.GetGeneration(pinned)}");

            // .NET 5 - .NET 7: triggers System.ArgumentException: Cannot use type 'Greet.Program+ClassWithoutReferenceField'. Only value types without pointers or references are supported.
            //var pinnedClassArray = GC.AllocateArray<ClassWithoutReferenceField>(100, true);
            //var pinnedStructArray = GC.AllocateArray<StructWithReferenceField>(100, true);
            //
            // these constraints are removed in .NET 8

            var pinnedGoodStructArray = GC.AllocateArray<StructWithoutReferenceField>(100, true);
            Console.WriteLine("\n\nWhere is the pinned array allocated?");
            Console.ReadLine();
            Console.WriteLine($"generation = {GC.GetGeneration(pinnedGoodStructArray)}");


            // list GCHandle
            //
            Console.ReadLine();
            Console.WriteLine("\n\nGCHandles");
            Console.WriteLine("---------------------------------------------------------");
            ListPinnedHandles();


            // list objects in POH
            //
            Console.ReadLine();
            Console.WriteLine("\n\nPOH");
            Console.WriteLine("---------------------------------------------------------");
            DumpPOH();

            Console.WriteLine();
        }

        private static void DumpPOH()
        {
            int pid = Process.GetCurrentProcess().Id;
            using (DataTarget target = DataTarget.AttachToProcess(pid, false))
            {
                foreach (ClrInfo clrInfo in target.ClrVersions)
                {
                    ClrRuntime runtime = clrInfo.CreateRuntime();
                    ClrHeap heap = runtime.Heap;
                    if (!heap.CanWalkHeap)
                    {
                        Console.WriteLine("Cannot walk heap...");
                        return;
                    }

                    foreach (ClrSubHeap subHeap in heap.SubHeaps)
                    {
                        Console.WriteLine($"heap #{subHeap.Index}");

                        foreach (var segment in subHeap.Segments.OrderBy(s => s.Start))
                        {
                            if (segment.Kind != GCSegmentKind.Pinned)
                                continue;

                            Console.WriteLine($"   {segment.Start,11:x} - {segment.End,11:x} ({segment.Length})");

                            foreach (var instance in segment.EnumerateObjects())
                            {
                                Console.WriteLine($"      {instance.Address,11:x} ({instance.Size,6}) | {instance.Type?.Name}");

                            }
                        }
                        Console.WriteLine();
                    }
                }
            }
       }

        private static void ListPinnedHandles()
        {
            int pid = Process.GetCurrentProcess().Id;
            using (DataTarget target = DataTarget.AttachToProcess(pid, false))
            {
                foreach (ClrInfo clrInfo in target.ClrVersions)
                {
                    ClrRuntime runtime = clrInfo.CreateRuntime();
                    ClrHeap heap = runtime.Heap;
                    if (!heap.CanWalkHeap)
                    {
                        Console.WriteLine("Cannot walk heap...");
                        return;
                    }

                    int currentSubHeapIndex = -1;
                    ulong currentSegmentStart = ulong.MaxValue;
                    bool isFirstSegment = true;
                    foreach (var handle in runtime
                        .EnumerateHandles()
                        .Where(h => (h.HandleKind == ClrHandleKind.Pinned) || (h.HandleKind == ClrHandleKind.AsyncPinned))
                        .OrderBy(h => heap.GetSegmentByAddress(h.Object.Address)?.SubHeap.Index)
                        .ThenBy(h => heap.GetSegmentByAddress(h.Object.Address)?.Start)
                        .ThenBy(h => h.Object.Address)
                        )
                    {
                        var instance = handle.Object;
                        if (instance.Address == 0)
                        {
                            // no more there
                            continue;
                        }

                        var segment = heap.GetSegmentByAddress(instance.Address);
                        if (segment == null)
                        {
                            continue;
                        }

                        var index = segment.SubHeap.Index;
                        if (index != currentSubHeapIndex)
                        {
                            currentSubHeapIndex = index;
                            if (index != 0)
                            {
                                Console.WriteLine("---------------------------\n");
                            }
                            Console.WriteLine($"heap #{index}");
                            isFirstSegment = true;
                        }

                        if (segment.Start != currentSegmentStart)
                        {
                            currentSegmentStart = segment.Start;

                            if (isFirstSegment)
                            {
                                isFirstSegment = false;
                            }
                            else
                            {
                                Console.WriteLine();
                            }

                            Console.WriteLine($"   {segment.Start,11:x} - {segment.End,11:x}");
                        }

                        var generation = segment.GetGeneration(instance.Address);
                        var gen =
                            (generation == Generation.Generation0) ? "gen 0" :
                            (generation == Generation.Generation1) ? "gen 1" :
                            (generation == Generation.Generation2) ? "gen 2" :
                            (generation == Generation.Large) ? " LOH" :
                            (generation == Generation.Pinned) ? " POH" :
                            (generation == Generation.Frozen) ? "NGCH" :
                            "?";

                        var kind = (handle.HandleKind == ClrHandleKind.Pinned) ? "P" : "A";
                        Console.WriteLine($"      {kind} - {gen} {instance.Address,11:x} ({instance.Size,6}) | {instance.Type?.Name}");
                    }
                }

            }

        }
    }
}
