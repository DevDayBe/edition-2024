using ListenerGui.Models;

namespace ListenerGui;

public class Frame
{
    public double PrivateMemoryMb { get; set; }

    public IReadOnlyList<SubHeap> SubHeaps { get; set; }

    public int GcNumber { get; set; }
}