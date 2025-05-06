using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace RW.Brutal;

/// <summary>
/// Provides functionality for managing and retrieving tooltips associated with specific keys.
/// </summary>
public static class ToolTips
{
    private const string PATH = "ToolTips.xml";
    private static readonly Dictionary<string, string> _toolTips;

    static ToolTips()
    {
        _toolTips = Load();
    }

    /// <summary>
    /// Loads tooltips from an XML file.
    /// </summary>
    /// <returns>
    /// A dictionary containing tooltips, where the key is the tooltip identifier and the value is the tooltip text.
    /// Returns an empty dictionary if the file does not exist or cannot be loaded.
    /// </returns>
    private static Dictionary<string, string> Load()
    {
        if (!File.Exists(PATH))
            return [];

        var serializer = new XmlSerializer(typeof(List<ToolTip>));
        using var reader = new StreamReader(PATH);
        var toolTips = (List<ToolTip>)serializer.Deserialize(reader);
        var map = new Dictionary<string, string>();

        foreach (var toolTip in toolTips)
            map.Add(toolTip.Key, toolTip.Text);

        return map;
    }

    /// <summary>
    /// Retrieves the tooltip text associated with a specific highlighting instance.
    /// </summary>
    /// <param name="highlighting">The highlighting instance for which the tooltip text is to be retrieved.</param>
    /// <returns>
    /// A string containing the tooltip text associated with the provided highlighting instance.
    /// Returns an empty string if no tooltip is defined for the given instance.
    /// </returns>
    public static string GetToolTip(IHighlighting highlighting)
    {
        var typeName = highlighting.GetType().Name;
        return GetToolTip(typeName);
    }

    /// <summary>
    /// Retrieves the tooltip text associated with the specified key.
    /// </summary>
    /// <param name="key">The key for which the tooltip text is to be retrieved.</param>
    /// <returns>
    /// A string containing the tooltip text associated with the specified key.
    /// Throws a <see cref="KeyNotFoundException"/> if no tooltip is defined for the provided key.
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown when the specified key does not exist in the tooltip dictionary.</exception>
    public static string GetToolTip(in string key)
    {
        return _toolTips.TryGetValue(key, out var toolTip)
            ? toolTip
            : throw new KeyNotFoundException(key);
    }
}

/// <summary>
/// Represents a tooltip with a unique key and corresponding text used for display or informational purposes.
/// </summary>
public class ToolTip
{
    public string Key { get; set; }
    public string Text { get; set; }
}

