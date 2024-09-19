using Microsoft.CodeAnalysis;
using SourceGen.Common;

namespace TypeWrap.SourceGen
{
    public static class GeneratorHelpers
    {
        private const string SKIP_ATTRIBUTE = "SkipGeneratorForAssemblyAttribute";

        public static bool IsValidCompilation(this Compilation compilation)
            => compilation.Assembly.HasAttributeSimple(SKIP_ATTRIBUTE) == false;
    }
}
