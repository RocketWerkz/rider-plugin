namespace RW.Brutal.Highlightings
{
    // Add a marker interface to all of our highlights. If we specify a baseClass in ErrorsGen, we have to provide an
    // actual class with an abstract IsValid method, because ErrorsGen will declare IsValid as an override.
    public abstract class CSharpBrutalHighlightingBase : IBrutalHighlighting
    {
        public abstract bool IsValid();
    }
}