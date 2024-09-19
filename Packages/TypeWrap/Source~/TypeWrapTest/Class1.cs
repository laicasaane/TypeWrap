using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TypeWrap;

namespace TypeWrapTest
{
    [WrapType(typeof(int))]
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

    public interface IMake<T>
    {
        T Make(T value);
    }

    public interface IDoTestStruct
    {
        TestStruct Do(TestStruct value);

        TestStruct ThisTest { get; }
    }

    [WrapRecord]
    public readonly partial record struct WrapDoTestStruct(IDoTestStruct _);

    public readonly struct TestStruct : IMake<TestStruct>
    {
        private readonly TestStruct[] _arr;

        public ref TestStruct ThisTest
        {
            get => ref _arr[0];
        }

        public TestStruct Do(TestStruct value)
        {
            return value;
        }

        public TestStruct Make(TestStruct value)
        {
            return value;
        }

        private void DoPrivate(int value) { }
    }

    [WrapRecord]
    public partial record struct TestStructWrapper(TestStruct _)
    {
        public void Do()
        {
            this._ = new TestStruct();
        }
    }

    public interface ITempCollection
    {
        object this[int index] { get; set; }

        void Add([NotNull] object item);
    }

    public interface ITempList : ITempCollection
    {
        new object this[int index] { get; set; }
    }

    public interface ITempCollection<T> : ITempCollection
    {
        new T this[int index] { get; set; }
    }

    public interface ITempList<T> : ITempList, ITempCollection<T>
    {
        new T this[int index] { get; set; }

        void Add([NotNull] T item);
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class MyAttribute : Attribute { }

    public class TempList<T> : ITempList<T>
    {
        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        object ITempList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        object ITempCollection.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public TempList<T> ThisTest => this;

        [My]
        public void Add([NotNull] T item)
        {
            throw new NotImplementedException();
        }

        public void Add([NotNull] object item)
        {
            throw new NotImplementedException();
        }

        public void Transform<TOut>()
            where TOut : class, IEquatable<T>, new()
        {
            throw new NotImplementedException();
        }
    }

    [WrapRecord]
    public partial record class TempListInt(TempList<int> Value);

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

    public class MyEvents
    {
        public event Action onEvent;

        public void DoSomething()
        {
            onEvent?.Invoke();
        }
    }

    [WrapType(typeof(MyEvents))]
    public partial class EventWrapper { }

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
