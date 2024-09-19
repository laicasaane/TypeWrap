using TypeWrap;

namespace AliasTests
{
    [WrapType(typeof(int))]
    public partial struct HeroId { }

    [WrapType(typeof(int), "wrappedValue")]
    public partial struct IntWrapper { }
}