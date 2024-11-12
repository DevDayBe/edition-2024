using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HeapViewer
{
    public class FreeBlock
    {
        public FreeBlock(ulong address, ulong size)
        {
            Address = address;
            Size = size;
        }
        public ulong Address;
        public ulong Size;
    }

    public class PinnedBlock
    {
        public PinnedBlock(ulong address, ulong size)
        {
            Address = address;
            Size = size;
        }
        public ulong Address;
        public ulong Size;
    }

    public class HeapModel
    {
        public string Name
        {
            get { return $"Heap #{Index}";  }
        }

        public int Index
        {
            get; set;
        }

        public List<SegmentModel> Segments
        {
            get; set;
        }
    }

    public class SegmentModel
    {
        private string GetHeapKind()
        {
            if (Kind == GCSegmentKind.Pinned)
            {
                return "POH";
            }
            else
            if (Kind == GCSegmentKind.Large)
            {
                return "LOH";
            }
            else
            if (Kind == GCSegmentKind.Generation0)
            {
                return "gen0";
            }
            else
            if (Kind == GCSegmentKind.Generation1)
            {
                return "gen1";
            }
            else
            if (Kind == GCSegmentKind.Generation2)
            {
                return "gen2";
            }
            else
            if (Kind == GCSegmentKind.Frozen)
            {
                return "NGCH";
            }
            else
                return "?";
        }

        public string KindAndIndex
        {
            get { return GetHeapKind() + " #" + HeapIndex.ToString(); }
            set { }
        }

        public string ShortKind
        {
            get => GetHeapKind();
        }

        public GridLength ControlWidth { get; set; }

        public GridLength EmptyColumnWidth { get; set; }

        public List<ulong> PinnedAddresses
        {
            get
            {
                return _pinnedBlocks.Select(pb => pb.Address).ToList();
            }
        }

        public List<PinnedBlock> PinnedBlocks
        {
            get
            {
                return _pinnedBlocks;
            }
        }

        public IReadOnlyList<FreeBlock> FreeBlocks { get; set; }

        public ulong Start { get; set; }

        public ulong End { get; set; }

        public ulong CommittedBytes
        {
            get => End - Start;
        }

        // gen0, gen1, gen2, LOH, POH (does not support NGCH)
        // !! could be a problem for .NET Framework where Ephemeral may mask gen0/gen1/gen2
        public GCSegmentKind Kind { get; set; }

        public int HeapIndex { get; set; }

        public void AddPinnedBlock(ulong address, ulong size)
        {
            _pinnedBlocks.Add(new PinnedBlock(address, size));
        }
        private List<PinnedBlock> _pinnedBlocks = new List<PinnedBlock>();
    }
}
