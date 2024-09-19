using System;

namespace TypeWrap
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class WrapTypeAttribute : Attribute
    {
        public const string DEFAULT_MEMBER_NAME = "value";

        public Type Type { get; }

        public string MemberName { get; }

        public bool IsPrivate { get; }

        public bool ExcludeConverter { get; set; }

        public WrapTypeAttribute(Type type) : this(type, DEFAULT_MEMBER_NAME)
        { }

        public WrapTypeAttribute(Type type, string memberName)
        {
            this.Type = type;
            this.MemberName = memberName;
        }
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class WrapRecordAttribute : Attribute
    {
        public bool ExcludeConverter { get; set; }
    }
}
