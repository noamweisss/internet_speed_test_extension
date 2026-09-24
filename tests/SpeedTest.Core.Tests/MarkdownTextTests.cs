using Xunit;

namespace SpeedTest.Core.Tests;

public sealed class MarkdownTextTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Example ISP", "Example ISP")]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("Tel Aviv, IL", "Tel Aviv, IL")]
    [InlineData("![x](p.png)", "\\!\\[x\\]\\(p.png\\)")]
    [InlineData("<img src=x>", "\\<img src=x\\>")]
    [InlineData("a*b_c`d|e~f#g\\h", "a\\*b\\_c\\`d\\|e\\~f\\#g\\\\h")]
    [InlineData("line1\r\nline2", "line1  line2")]
    public void Escape_NeutralisesMarkdownAndHtml(string? input, string expected) => Assert.Equal(expected, MarkdownText.Escape(input));
}
