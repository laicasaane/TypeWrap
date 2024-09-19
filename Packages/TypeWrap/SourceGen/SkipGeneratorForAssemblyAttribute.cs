using System;

namespace TypeWrap.SourceGen
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class SkipGeneratorForAssemblyAttribute : Attribute { }
}
