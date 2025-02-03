using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace ReSharperPlugin.EntsPlugin
{
    static class KnownTypes
    {
        public static readonly IClrTypeName GD = new ClrTypeName("GD");
        public static readonly IClrTypeName ResourceLoader = new ClrTypeName("ResourceLoader");
        public static readonly IClrTypeName Float4 = new ClrTypeName("Brutal.float4");
    }
}