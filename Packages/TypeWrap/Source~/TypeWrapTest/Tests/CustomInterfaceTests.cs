#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter

using TypeWrap;

namespace TypeWrapTest
{
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
}
