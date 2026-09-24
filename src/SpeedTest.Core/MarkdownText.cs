using System.Text;

namespace SpeedTest.Core;

/// <summary>
/// Escapes server-supplied text before it is placed in markdown, so a value like an image link cannot make the
/// renderer fetch anything or change the layout (docs/SAFETY-CONTRACT.md, reviewer question 5).
/// </summary>
public static class MarkdownText
{
    private const string Specials = "\\`*_[]()<>!|~#";

    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var text = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            if (char.IsControl(ch))
            {
                text.Append(' ');
            }
            else
            {
                if (Specials.Contains(ch))
                {
                    text.Append('\\');
                }

                text.Append(ch);
            }
        }

        return text.ToString();
    }
}
