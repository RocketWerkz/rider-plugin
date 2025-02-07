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
        public static readonly IClrTypeName float3 = new ClrTypeName("Brutal.float3");
    }
}