using System.Text;
using System.Text.RegularExpressions;
using Yarn.Unity;

/// <summary>Shared raw-text syntax for ordinary lines, options and the legacy last-line view.</summary>
public static class PerceptionText
{
    private static readonly Regex ColorMarkers = new Regex(@"\*(.+?)\*|~(.+?)~|\+(.+?)\+|=(.+?)=");

    public static bool TryGetRaw(LocalizedLine line, out string raw)
    {
        raw = null;
        if (line == null) return false;
        foreach (var attribute in line.TextWithoutCharacterName.Attributes)
        {
            if (attribute.Name == "raw" && attribute.Properties.TryGetValue("text", out var value))
            {
                raw = value.StringValue;
                return true;
            }
        }
        foreach (var metadata in line.Metadata ?? System.Array.Empty<string>())
        {
            if (!metadata.StartsWith("raw:", System.StringComparison.Ordinal)) continue;
            raw = metadata.Substring(4).Replace("_", " ");
            return true;
        }
        return false;
    }

    public static string Colorize(string input)
    {
        input ??= string.Empty;
        var result = new StringBuilder();
        result.Append("<color=#4A7C3F>");
        int previous = 0;
        foreach (Match match in ColorMarkers.Matches(input))
        {
            result.Append(input, previous, match.Index - previous);
            string color;
            string text;
            if (match.Groups[1].Success) { color = "#FF0000"; text = match.Groups[1].Value; }
            else if (match.Groups[2].Success) { color = "#FFFF00"; text = match.Groups[2].Value; }
            else if (match.Groups[3].Success) { color = "#FF69B4"; text = match.Groups[3].Value; }
            else { color = "#4B0082"; text = match.Groups[4].Value; }
            result.Append("<color=").Append(color).Append('>').Append(text).Append("</color>");
            previous = match.Index + match.Length;
        }
        result.Append(input, previous, input.Length - previous);
        return result.Append("</color>").ToString();
    }
}
