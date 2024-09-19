using System;
using TypeWrap;

namespace TypeWrapTest
{
    [WrapType(typeof(Span<int>), "Values")]
    public readonly ref partial struct MySpanInt
    {
        public readonly Span<int> Values;
    }

    public ref struct RefX
    {
        public readonly bool Equals(RefX other)
            => false;
    }

    [WrapType(typeof(RefX))]
    public readonly ref partial struct RefXWrapper { }

    public readonly record struct ValueTemp(int Value)
    {
        public static float operator +(ValueTemp left)
            => left.Value;
    }
}
