#nullable enable

using System.Diagnostics.CodeAnalysis;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace ReSharperPlugin.EntsPlugin
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class KnownTypes
    {
        public static readonly IClrTypeName GD = new ClrTypeName("GD");
        public static readonly IClrTypeName ResourceLoader = new ClrTypeName("ResourceLoader");
        
        public static readonly IClrTypeName Float3 = new ClrTypeName("Brutal.float3");
        public static readonly IClrTypeName Float4 = new ClrTypeName("Brutal.float4");
        
        public static readonly IClrTypeName Byte3 = new ClrTypeName("Brutal.byte3");
        public static readonly IClrTypeName Byte4 = new ClrTypeName("Brutal.byte4");
        
        public static readonly IClrTypeName Ushort3 = new ClrTypeName("Brutal.ushort3");
        public static readonly IClrTypeName Ushort4 = new ClrTypeName("Brutal.ushort4");
    }
}