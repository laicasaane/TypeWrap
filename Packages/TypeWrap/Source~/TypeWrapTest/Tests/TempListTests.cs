using System;
using System.Diagnostics.CodeAnalysis;
using TypeWrap;

namespace TypeWrapTest
{
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
}
