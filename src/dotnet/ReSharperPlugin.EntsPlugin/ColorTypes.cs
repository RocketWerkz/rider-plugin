using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Colors;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace ReSharperPlugin.EntsPlugin
{
    /// <summary>
    /// Helps manage and cache color-related type elements.
    /// </summary>
    public class ColorTypes
    {
        private static readonly Key<ColorTypes> ourColorTypesKey = new("ColorTypes");

        public static ColorTypes GetInstance(IPsiModule module)
        {
            var colorTypes = module.GetData(ourColorTypesKey);
            if (colorTypes == null)
            {
                colorTypes = new ColorTypes(module);
                module.PutData(ourColorTypesKey, colorTypes);
            }

            return colorTypes;
        }

        private ColorTypes([NotNull] IPsiModule module)
        {
            var cache = module.GetPsiServices().Symbols.GetSymbolScope(module, true, true);
            
            // Only keep ColorType and not Color32Type
            ColorType = cache.GetTypeElementByCLRName(KnownTypes.Float4);
        }

        // Naming was previously references to color-related type `UnityEngine.Color` and `UnityEngine.Color32`
        [CanBeNull] public ITypeElement ColorType { get; }

        /// <summary>
        ///     Checks if `typeElement` is `ColorType`.
        /// </summary>
        public bool IsColorType([CanBeNull] ITypeElement typeElement)
        {
            return ColorType != null && ColorType.Equals(typeElement);
        }
    }
}