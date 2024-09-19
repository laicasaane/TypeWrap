using TypeWrap;
using UnityEngine;

namespace TypeWrapTests
{
    [WrapType(typeof(int))]
    public partial struct HeroId { }

    [WrapType(typeof(int), "wrappedValue")]
    public partial struct IntWrapper { }

    [WrapRecord]
    public readonly partial record struct Coord2D(Vector2Int _);
}