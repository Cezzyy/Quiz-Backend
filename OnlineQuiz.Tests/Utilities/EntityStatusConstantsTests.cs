using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Utilities;

public class EntityStatusConstantsTests
{
    [Fact]
    public void Constants_HaveCorrectValues()
    {
        Assert.Equal("Active", EntityStatusConstants.Active);
        Assert.Equal("Archived", EntityStatusConstants.Archived);
        Assert.Equal("Inactive", EntityStatusConstants.Inactive);
    }

    [Fact]
    public void ValidStatuses_ContainsAllThreeStatuses()
    {
        Assert.Contains(EntityStatusConstants.Active, EntityStatusConstants.ValidStatuses);
        Assert.Contains(EntityStatusConstants.Archived, EntityStatusConstants.ValidStatuses);
        Assert.Contains(EntityStatusConstants.Inactive, EntityStatusConstants.ValidStatuses);
        Assert.Equal(3, EntityStatusConstants.ValidStatuses.Length);
    }

    [Theory]
    [InlineData("Active", true)]
    [InlineData("Archived", true)]
    [InlineData("Inactive", true)]
    [InlineData("active", true)]      // case-insensitive
    [InlineData("ARCHIVED", true)]    // case-insensitive
    [InlineData("inactive", true)]    // case-insensitive
    [InlineData("Deleted", false)]
    [InlineData("Pending", false)]
    [InlineData("", false)]
    [InlineData("Unknown", false)]
    public void IsValidStatus_ReturnsExpected(string status, bool expected)
    {
        Assert.Equal(expected, EntityStatusConstants.IsValidStatus(status));
    }
}
