using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace ReSharperPlugin.EntsPlugin
{
    static class BrutalTypes
    {
        private static IClrTypeName BrutalTypeName(string typeName)
            => new ClrTypeName($"Brutal.{typeName}");
        
        public static readonly IClrTypeName GD                    = BrutalTypeName("GD");
        public static readonly IClrTypeName ResourceLoader        = BrutalTypeName("ResourceLoader");
        public static readonly IClrTypeName Texture               = BrutalTypeName("Texture");
    }
}