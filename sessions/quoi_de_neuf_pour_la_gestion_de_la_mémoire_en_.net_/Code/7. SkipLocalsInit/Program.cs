using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace _SkipLocalsInit;

internal class Program
{
    static void Main(string[] args)
    {
        RuntimeHelpers.PrepareMethod(typeof(Program).GetMethod(nameof(Method1), BindingFlags.Static | BindingFlags.NonPublic).MethodHandle);
        RuntimeHelpers.PrepareMethod(typeof(Program).GetMethod(nameof(Method2), BindingFlags.Static | BindingFlags.NonPublic).MethodHandle);
        RuntimeHelpers.PrepareMethod(typeof(Program).GetMethod(nameof(Method3), BindingFlags.Static | BindingFlags.NonPublic).MethodHandle);

        Method1();
        Method2();

        Method3();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Method1()
    {
        Span<char> str = stackalloc char[] { 'h', 'e', 'l', 'l', 'o' };
    }


    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Method2()
    {
        Span<char> str = stackalloc char[5];

        Console.WriteLine(new string(str));
    }

    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe SomeStructWithPadding Method3()
    {
        SomeStructWithPadding obj1;
        obj1.Value = 0;

        SomeStructWithPadding obj2;
        obj2.Value = 0;

        Console.WriteLine(Unsafe.AsRef<LongStruct>(&obj2).Value2);
        Console.WriteLine(Unsafe.AsRef<LongStruct>(&obj2).Value3);

        return obj1;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct SomeStructWithPadding
    {
        [FieldOffset(0)]
        public long Value;
    }

    internal struct LongStruct
    {
        public long Value1;
        public char Value2;
        public char Value3;
    }
}