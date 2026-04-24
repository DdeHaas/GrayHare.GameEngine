using GrayHare.GameEngine.Shaders;

namespace GrayHare.GameEngine.Tests.Shaders;

public sealed class GlslVersionParserTests
{
    [Fact]
    public void Parse_WithVersionDirective_ReturnsVersionNumber()
    {
        const string source = "#version 460\nvoid main() {}";

        int? result = GlslVersionParser.Parse(source);

        Assert.Equal(460, result);
    }

    [Fact]
    public void Parse_WithProfileToken_IgnoresProfileAndReturnsVersionNumber()
    {
        const string source = "#version 460 core\nvoid main() {}";

        int? result = GlslVersionParser.Parse(source);

        Assert.Equal(460, result);
    }

    [Fact]
    public void Parse_WithLeadingWhitespace_ReturnsVersionNumber()
    {
        const string source = "   #version 330\nvoid main() {}";

        int? result = GlslVersionParser.Parse(source);

        Assert.Equal(330, result);
    }

    [Fact]
    public void Parse_WithNoDirective_ReturnsNull()
    {
        const string source = "void main() {}";

        int? result = GlslVersionParser.Parse(source);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithEmptyString_ReturnsNull()
    {
        int? result = GlslVersionParser.Parse(string.Empty);

        Assert.Null(result);
    }
}
