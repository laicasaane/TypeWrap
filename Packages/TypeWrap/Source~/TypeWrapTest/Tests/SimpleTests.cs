using System;
using System.Collections.Generic;
using TypeWrap;
using UnityEngine;

namespace TypeWrapTest
{
    [WrapType(typeof(int), ExcludeConverter = true)]
    public partial struct IntWrapper
    {
        public readonly override int GetHashCode()
        {
            return value.GetHashCode();
        }
    }

    [WrapRecord]
    public readonly partial record struct BoolWrapper(bool _)
    {
        //public static bool operator true(BoolWrapper value)
        //{
        //    return value._;
        //}

        //public static bool operator false(BoolWrapper value)
        //{
        //    return value._;
        //}
    }

    [WrapType(typeof(AttributeTargets))]
    public readonly partial struct EnumWrapper { }

    [WrapRecord]
    public readonly partial record struct HeroId(int _);

    

    [AttributeUsage(AttributeTargets.All)]
    public sealed class MyAttribute : Attribute { }

    

    [WrapType(typeof(List<int>))]
    public partial class ListInt { }

    [WrapRecord]
    public readonly partial record struct FloatWrapper(float Value)// : IConvertible
    {
        public TypeCode GetTypeCode()
        {
            return Convert.GetTypeCode(Value);
        }
    }

    [WrapRecord]
    public partial record class ListFloat(List<float> Value);

    [WrapRecord]
    public partial record class ListT<T>(List<T> Value);

    [WrapRecord]
    public partial record class Map<TKey, TValue>(Dictionary<TKey, TValue> Value);

    [WrapRecord]
    public readonly partial record struct Coord2D(Vector2Int _);
}
