// using Strings = JetBrains.ReSharper.Plugins.Unity.Resources.Strings;

using System.Drawing;
using JetBrains.ReSharper.Feature.Services.Daemon.Attributes;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.TextControl.DocumentMarkup.VisualStudio;

namespace RW.Brutal
{
    // Highlighters define the look of a highlighting. They belong to a group, which is used to create the Settings
    // page in the IDE.
    [RegisterHighlighterGroup(GROUP_ID, "Brutal",
        HighlighterGroupPriority.LANGUAGE_SETTINGS,
        Language = typeof(CSharpLanguage),
        DemoText = DEMO_TEXT,
        RiderNamesProviderType = typeof(BrutalHighlighterNamesProvider))]

    // Define the highlighter, which describe how a highlighting is displayed.
    // Applying Bold Font Text Styling to code text.
    [RegisterHighlighter(BRUTAL_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE,
        GroupId = GROUP_ID,
        EffectType = EffectType.TEXT,
        FontStyle = FontStyle.Bold,
        // RiderPresentableNameResourceType = typeof(Strings),
        // RiderPresentableNameResourceName = nameof(Strings.ImplicitlyUsedIdentifier_RiderPresentableName),
        FallbackAttributeId = DefaultLanguageAttributeIds.IDENTIFIER,
        VsGenerateClassificationDefinition = VsGenerateDefinition.VisibleClassification,
        Layer = HighlighterLayer.SYNTAX + 1)]
    
    public static class BrutalHighlightingAttributeIds
    {
        public const string GROUP_ID = "Brutal";

        // All attributes should begin with "ReSharper Cg ". See CgHighlighterNamesProvider below
        public const string BRUTAL_IMPLICITLY_USED_IDENTIFIER_ATTRIBUTE = "ReSharper Brutal Implicitly used identifier";
        
        // The internal action Dump Rider Highlighters Tree is very helpful, but it doesn't include a lot of the C#
        // highlights and will include e.g. dead code highlights. You'll have to edit manually.
        // When editing, it's useful to inject XML, but be very careful of the angle brackets in `GetComponent<Grid>`!
        private const string DEMO_TEXT =
@"<CSHARP_KEYWORD>public</CSHARP_KEYWORD> <CSHARP_KEYWORD>class</CSHARP_KEYWORD> <CSHARP_CLASS_IDENTIFIER><BRUTAL_IMPLICITLY_USED_IDENTIFIER>MyMonoBehaviour</BRUTAL_IMPLICITLY_USED_IDENTIFIER></CSHARP_CLASS_IDENTIFIER> : <CSHARP_CLASS_IDENTIFIER>MonoBehaviour</CSHARP_CLASS_IDENTIFIER>
<CSHARP_BRACES>{</CSHARP_BRACES>
  <CSHARP_LINE_COMMENT>// This method is called very frequently by Brutal</CSHARP_LINE_COMMENT>
  <CSHARP_KEYWORD>public</CSHARP_KEYWORD> <CSHARP_KEYWORD>void</CSHARP_KEYWORD> <CSHARP_METHOD_IDENTIFIER><BRUTAL_IMPLICITLY_USED_IDENTIFIER>Update</BRUTAL_IMPLICITLY_USED_IDENTIFIER></CSHARP_METHOD_IDENTIFIER><CSHARP_PARENTHESES>()</CSHARP_PARENTHESES>
  <CSHARP_BRACES>{</CSHARP_BRACES>
    <CSHARP_LINE_COMMENT>// Camera.main is inefficient inside a performance critical context</CSHARP_LINE_COMMENT>
    <CSHARP_KEYWORD>var</CSHARP_KEYWORD> <CSHARP_LOCAL_VARIABLE_IDENTIFIER>camera</CSHARP_LOCAL_VARIABLE_IDENTIFIER> = <BRUTAL_EXPENSIVE_CAMERA_MAIN_USAGE><CSHARP_CLASS_IDENTIFIER>Camera</CSHARP_CLASS_IDENTIFIER>.<CSHARP_STATIC_PROPERTY_IDENTIFIER>main</CSHARP_STATIC_PROPERTY_IDENTIFIER></BRUTAL_EXPENSIVE_CAMERA_MAIN_USAGE><CSHARP_SEMICOLON>;</CSHARP_SEMICOLON>

    <CSHARP_LINE_COMMENT>// GetComponent is expensive inside a performance critical context</CSHARP_LINE_COMMENT>
    <CSHARP_LINE_COMMENT>// Comparison to null for Brutal types transitions to native code</CSHARP_LINE_COMMENT>
    <CSHARP_KEYWORD>var</CSHARP_KEYWORD> <CSHARP_LOCAL_VARIABLE_IDENTIFIER>grid</CSHARP_LOCAL_VARIABLE_IDENTIFIER> = <CSHARP_METHOD_IDENTIFIER><BRUTAL_EXPENSIVE_METHOD_INVOCATION>GetComponent</BRUTAL_EXPENSIVE_METHOD_INVOCATION></CSHARP_METHOD_IDENTIFIER><<CSHARP_CLASS_IDENTIFIER>Grid</CSHARP_CLASS_IDENTIFIER>><CSHARP_PARENTHESES>()</CSHARP_PARENTHESES><CSHARP_SEMICOLON>;</CSHARP_SEMICOLON>
    <CSHARP_KEYWORD>if</CSHARP_KEYWORD> <CSHARP_PARENTHESES>(</CSHARP_PARENTHESES><CSHARP_LOCAL_VARIABLE_IDENTIFIER>grid</CSHARP_LOCAL_VARIABLE_IDENTIFIER> <CSHARP_OVERLOADED_OPERATOR><BRUTAL_EXPENSIVE_NULL_COMPARISON>==</BRUTAL_EXPENSIVE_NULL_COMPARISON></CSHARP_OVERLOADED_OPERATOR> <CSHARP_KEYWORD>null</CSHARP_KEYWORD><CSHARP_PARENTHESES>)</CSHARP_PARENTHESES>
    <CSHARP_BRACES>{</CSHARP_BRACES>
      <CSHARP_LINE_COMMENT>// ...</CSHARP_LINE_COMMENT>
    <CSHARP_BRACES>}</CSHARP_BRACES>

    <CSHARP_LINE_COMMENT>// Multidimensional array access is a method call, not an intrinsic operation</CSHARP_LINE_COMMENT>
    <CSHARP_KEYWORD>int</CSHARP_KEYWORD><CSHARP_BRACKETS>[</CSHARP_BRACKETS>,<CSHARP_BRACKETS>]</CSHARP_BRACKETS> <CSHARP_LOCAL_VARIABLE_IDENTIFIER><BRUTAL_INEFFICIENT_MULTIDIMENSIONAL_ARRAY_USAGE>intArray</BRUTAL_INEFFICIENT_MULTIDIMENSIONAL_ARRAY_USAGE></CSHARP_LOCAL_VARIABLE_IDENTIFIER> = <CSHARP_KEYWORD>new</CSHARP_KEYWORD> <CSHARP_KEYWORD>int</CSHARP_KEYWORD><CSHARP_BRACKETS>[</CSHARP_BRACKETS><CSHARP_NUMBER>42</CSHARP_NUMBER>,<CSHARP_NUMBER>42</CSHARP_NUMBER><CSHARP_BRACKETS>]</CSHARP_BRACKETS><CSHARP_SEMICOLON>;</CSHARP_SEMICOLON>
    <CSHARP_KEYWORD>var</CSHARP_KEYWORD> <CSHARP_LOCAL_VARIABLE_IDENTIFIER>i1</CSHARP_LOCAL_VARIABLE_IDENTIFIER> = <BRUTAL_INEFFICIENT_MULTIDIMENSIONAL_ARRAY_USAGE><CSHARP_LOCAL_VARIABLE_IDENTIFIER>intArray</CSHARP_LOCAL_VARIABLE_IDENTIFIER><CSHARP_BRACKETS>[</CSHARP_BRACKETS><CSHARP_NUMBER>0</CSHARP_NUMBER>, <CSHARP_NUMBER>1</CSHARP_NUMBER><CSHARP_BRACKETS>]</CSHARP_BRACKETS></BRUTAL_INEFFICIENT_MULTIDIMENSIONAL_ARRAY_USAGE><CSHARP_SEMICOLON>;</CSHARP_SEMICOLON>
    <CSHARP_KEYWORD>var</CSHARP_KEYWORD> <CSHARP_LOCAL_VARIABLE_IDENTIFIER>i2</CSHARP_LOCAL_VARIABLE_IDENTIFIER> = <BRUTAL_INEFFICIENT_MULTIDIMENSIONAL_ARRAY_USAGE><CSHARP_LOCAL_VARIABLE_IDENTIFIER>intArray</CSHARP_LOCAL_VARIABLE_IDENTIFIER><CSHARP_BRACKETS>[</CSHARP_BRACKETS><CSHARP_NUMBER>0</CSHARP_NUMBER>, <CSHARP_NUMBER>2</CSHARP_NUMBER><CSHARP_BRACKETS>]</CSHARP_BRACKETS></BRUTAL_INEFFICIENT_MULTIDIMENSIONAL_ARRAY_USAGE><CSHARP_SEMICOLON>;</CSHARP_SEMICOLON>

    <CSHARP_LINE_COMMENT>// Multiplying a vector by more than one scalar takes up to 3 multiplications per scalar</CSHARP_LINE_COMMENT>
    <CSHARP_LINE_COMMENT>// Perform all scalar multiplications first</CSHARP_LINE_COMMENT>
    <CSHARP_KEYWORD>var</CSHARP_KEYWORD> <CSHARP_LOCAL_VARIABLE_IDENTIFIER>v1</CSHARP_LOCAL_VARIABLE_IDENTIFIER> = <CSHARP_KEYWORD>new</CSHARP_KEYWORD> <CSHARP_STRUCT_IDENTIFIER>Vector3</CSHARP_STRUCT_IDENTIFIER><CSHARP_PARENTHESES>(</CSHARP_PARENTHESES><CSHARP_NUMBER>1</CSHARP_NUMBER>, <CSHARP_NUMBER>0</CSHARP_NUMBER><CSHARP_PARENTHESES>)</CSHARP_PARENTHESES><CSHARP_SEMICOLON>;</CSHARP_SEMICOLON>
    <CSHARP_KEYWORD>var</CSHARP_KEYWORD> <CSHARP_LOCAL_VARIABLE_IDENTIFIER>result</CSHARP_LOCAL_VARIABLE_IDENTIFIER> = <BRUTAL_INEFFICIENT_MULTIPLICATION_ORDER><CSHARP_LOCAL_VARIABLE_IDENTIFIER>v1</CSHARP_LOCAL_VARIABLE_IDENTIFIER> <CSHARP_OVERLOADED_OPERATOR>*</CSHARP_OVERLOADED_OPERATOR> <CSHARP_LOCAL_VARIABLE_IDENTIFIER>i1</CSHARP_LOCAL_VARIABLE_IDENTIFIER> <CSHARP_OVERLOADED_OPERATOR>*</CSHARP_OVERLOADED_OPERATOR> <CSHARP_LOCAL_VARIABLE_IDENTIFIER>i2</CSHARP_LOCAL_VARIABLE_IDENTIFIER></BRUTAL_INEFFICIENT_MULTIPLICATION_ORDER><CSHARP_SEMICOLON>;</CSHARP_SEMICOLON>
        
    <CSHARP_LINE_COMMENT>// ...</CSHARP_LINE_COMMENT>
  <CSHARP_BRACES>}</CSHARP_BRACES>
<CSHARP_BRACES>}</CSHARP_BRACES>
";

// #region Original C# snippet
/*

public class MyMonoBehaviour : MonoBehaviour
{
    public void Update()
    {
        // Camera.main is inefficient inside a frequently called method
        var camera = Camera.main;

        // GetComponent is inefficient inside a frequently called method
        // Comparison to null for Unity types transitions to native code
        var grid = GetComponent<Grid>();
        if (grid == null)
        {
            // ...
        }

        // Multidimensional array access is a method call, not an intrinsic operation
        int[,] intArray = new int[42,42];
        var i1 = intArray[0, 1];
        var i2 = intArray[0, 2];

        // Multiplying a vector by more than one scalar takes up to 3 multiplications per scalar
        // Perform all scalar multiplications first
        var v1 = new Vector3(1, 0);
        var result = v1 * i1 * i2;

        // ...
    }
}

 */
// #endregion
    }
    
    // Convert the ReSharper/Visual Studio friendly IDs into IDEA friendly names. For R# compatibility, all attribute
    // IDs should start with "ReSharper" + some context specific prefix.
    // GetPresentableName will return the attribute ID with attributeIdPrefix or "ReSharper" stripped
    // GetHighlighterTag (for document markup) will return tagPrefix + the uppercase presentable name, with '.' replaced with '_'
    // GetExternalName (for saving) will return "ReSharper." + highlighter tag. For compatibility, don't change this.
    public class BrutalHighlighterNamesProvider : PrefixBasedSettingsNamesProvider
    {
        // Strips "ReSharper Brutal" from presentable name, uses "BRUTAL_{UPPERCASE_ATTRIBUTE_ID}" for document markup
        // and saving IDEA settings.
        public BrutalHighlighterNamesProvider()
            : base("ReSharper Brutal", "BRUTAL")
        {
        }

        // Return "BRUTAL_{UPPERCASE_ATTRIBUTE_ID}" instead of "ReSharper.BRUTAL_{UPPERCASE_ATTRIBUTE_ID}". We don't
        // need the "ReSharper." prefix as BRUTAL will not clash with anything in IDEA.
        public override string GetExternalName(string attributeId) => GetHighlighterTag(attributeId);
    }
}
