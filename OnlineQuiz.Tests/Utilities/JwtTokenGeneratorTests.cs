using System.Security.Claims;
using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Utilities;

public class JwtTokenGeneratorTests
{
    private const string SecretKey = "super-secret-key-for-tests-1234567890";
    private const string Issuer = "OnlineQuiz.TestIssuer";
    private const string Audience = "OnlineQuiz.TestAudience";

    [Fact]
    public void GenerateToken_And_ValidateToken_ReturnsPrincipalWithExpectedClaims()
    {
        var token = JwtTokenGenerator.GenerateToken(
            userId: 42,
            email: "student@example.com",
            roleId: 7,
            roleName: "Student",
            secretKey: SecretKey,
            issuer: Issuer,
            audience: Audience,
            expirationHours: 1);

        Assert.False(string.IsNullOrWhiteSpace(token));

        var principal = JwtTokenGenerator.ValidateToken(token, SecretKey, Issuer, Audience);

        Assert.NotNull(principal);

        Assert.Equal("42", JwtTokenGenerator.GetClaimValue(principal!, ClaimTypes.NameIdentifier));
        Assert.Equal("student@example.com", JwtTokenGenerator.GetClaimValue(principal!, ClaimTypes.Email));
        Assert.Equal("Student", JwtTokenGenerator.GetClaimValue(principal!, ClaimTypes.Role));

        Assert.Equal(42, JwtTokenGenerator.GetUserId(principal!));
        Assert.Equal(7, JwtTokenGenerator.GetRoleId(principal!));
    }

    [Fact]
    public void ValidateToken_ReturnsNull_ForInvalidSignature()
    {
        var token = JwtTokenGenerator.GenerateToken(
            userId: 1,
            email: "user@example.com",
            roleId: 1,
            roleName: "Admin",
            secretKey: SecretKey,
            issuer: Issuer,
            audience: Audience,
            expirationHours: 1);

        var wrongSecret = "another-secret-key-not-matching";

        var principal = JwtTokenGenerator.ValidateToken(token, wrongSecret, Issuer, Audience);

        Assert.Null(principal);
    }
}

