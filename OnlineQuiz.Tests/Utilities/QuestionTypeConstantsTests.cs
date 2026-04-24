using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Utilities;

public class QuestionTypeConstantsTests
{
    [Fact]
    public void Constants_HaveCorrectValues()
    {
        Assert.Equal("Single", QuestionTypeConstants.Single);
        Assert.Equal("Multiple", QuestionTypeConstants.Multiple);
        Assert.Equal("Text", QuestionTypeConstants.Text);
    }

    [Fact]
    public void ValidTypes_ContainsAllThreeTypes()
    {
        Assert.Contains(QuestionTypeConstants.Single, QuestionTypeConstants.ValidTypes);
        Assert.Contains(QuestionTypeConstants.Multiple, QuestionTypeConstants.ValidTypes);
        Assert.Contains(QuestionTypeConstants.Text, QuestionTypeConstants.ValidTypes);
        Assert.Equal(3, QuestionTypeConstants.ValidTypes.Length);
    }

    [Theory]
    [InlineData("Single", true)]
    [InlineData("Multiple", true)]
    [InlineData("Text", true)]
    [InlineData("single", true)]      // case-insensitive
    [InlineData("MULTIPLE", true)]    // case-insensitive
    [InlineData("Essay", false)]
    [InlineData("TrueFalse", false)]
    [InlineData("", false)]
    [InlineData("Unknown", false)]
    public void IsValid_ReturnsExpected(string type, bool expected)
    {
        Assert.Equal(expected, QuestionTypeConstants.IsValid(type));
    }

    [Theory]
    [InlineData("Single", true)]
    [InlineData("Multiple", true)]
    [InlineData("Text", false)]    // essay — no choices
    [InlineData("Essay", false)]   // unknown — no choices
    public void RequiresChoices_ReturnsExpected(string type, bool expected)
    {
        Assert.Equal(expected, QuestionTypeConstants.RequiresChoices(type));
    }

    [Theory]
    [InlineData("Text", true)]
    [InlineData("Single", false)]
    [InlineData("Multiple", false)]
    [InlineData("Essay", false)]   // unknown type — not essay
    public void IsEssayType_ReturnsExpected(string type, bool expected)
    {
        Assert.Equal(expected, QuestionTypeConstants.IsEssayType(type));
    }

    [Fact]
    public void SingleAndMultiple_RequireChoices_TextDoesNot()
    {
        Assert.True(QuestionTypeConstants.RequiresChoices(QuestionTypeConstants.Single));
        Assert.True(QuestionTypeConstants.RequiresChoices(QuestionTypeConstants.Multiple));
        Assert.False(QuestionTypeConstants.RequiresChoices(QuestionTypeConstants.Text));
    }

    [Fact]
    public void OnlyText_IsEssayType()
    {
        Assert.True(QuestionTypeConstants.IsEssayType(QuestionTypeConstants.Text));
        Assert.False(QuestionTypeConstants.IsEssayType(QuestionTypeConstants.Single));
        Assert.False(QuestionTypeConstants.IsEssayType(QuestionTypeConstants.Multiple));
    }
}
