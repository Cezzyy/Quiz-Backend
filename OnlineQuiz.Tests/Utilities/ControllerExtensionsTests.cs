using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Utilities;

// Minimal concrete controller to test extension methods
internal class StubController : ControllerBase
{
    public StubController(ClaimsPrincipal user)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}

public class ControllerExtensionsTests
{
    private static StubController WithClaims(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new StubController(new ClaimsPrincipal(identity));
    }

    private static StubController WithNoClaims()
        => new StubController(new ClaimsPrincipal(new ClaimsIdentity()));

    // ── TryGetAuthenticatedUserId ─────────────────────────────────────────

    [Fact]
    public void TryGetAuthenticatedUserId_ReturnsTrue_WithNameIdentifierClaim()
    {
        var controller = WithClaims(new Claim(ClaimTypes.NameIdentifier, "42"));
        var result = controller.TryGetAuthenticatedUserId(out int userId);
        Assert.True(result);
        Assert.Equal(42, userId);
    }

    [Fact]
    public void TryGetAuthenticatedUserId_ReturnsTrue_WithIdClaim()
    {
        var controller = WithClaims(new Claim("id", "99"));
        var result = controller.TryGetAuthenticatedUserId(out int userId);
        Assert.True(result);
        Assert.Equal(99, userId);
    }

    [Fact]
    public void TryGetAuthenticatedUserId_ReturnsTrue_WithUserIdClaim()
    {
        var controller = WithClaims(new Claim("UserId", "7"));
        var result = controller.TryGetAuthenticatedUserId(out int userId);
        Assert.True(result);
        Assert.Equal(7, userId);
    }

    [Fact]
    public void TryGetAuthenticatedUserId_PrefersNameIdentifier_OverOtherClaims()
    {
        // NameIdentifier should take priority
        var controller = WithClaims(
            new Claim(ClaimTypes.NameIdentifier, "10"),
            new Claim("id", "20"),
            new Claim("UserId", "30"));
        var result = controller.TryGetAuthenticatedUserId(out int userId);
        Assert.True(result);
        Assert.Equal(10, userId);
    }

    [Fact]
    public void TryGetAuthenticatedUserId_ReturnsFalse_WhenNoClaims()
    {
        var controller = WithNoClaims();
        var result = controller.TryGetAuthenticatedUserId(out int userId);
        Assert.False(result);
        Assert.Equal(0, userId);
    }

    [Fact]
    public void TryGetAuthenticatedUserId_ReturnsFalse_WhenClaimIsNotInteger()
    {
        var controller = WithClaims(new Claim(ClaimTypes.NameIdentifier, "not-a-number"));
        var result = controller.TryGetAuthenticatedUserId(out int userId);
        Assert.False(result);
        Assert.Equal(0, userId);
    }

    // ── GetAuthenticatedUserId ────────────────────────────────────────────

    [Fact]
    public void GetAuthenticatedUserId_ReturnsUserId_WhenClaimPresent()
    {
        var controller = WithClaims(new Claim(ClaimTypes.NameIdentifier, "55"));
        var userId = controller.GetAuthenticatedUserId();
        Assert.Equal(55, userId);
    }

    [Fact]
    public void GetAuthenticatedUserId_Throws_WhenNoClaims()
    {
        var controller = WithNoClaims();
        Assert.Throws<UnauthorizedAccessException>(() => controller.GetAuthenticatedUserId());
    }

    [Fact]
    public void GetAuthenticatedUserId_Throws_WhenClaimIsNotInteger()
    {
        var controller = WithClaims(new Claim(ClaimTypes.NameIdentifier, "abc"));
        Assert.Throws<UnauthorizedAccessException>(() => controller.GetAuthenticatedUserId());
    }

    // ── ValidateUserRole ──────────────────────────────────────────────────

    [Fact]
    public void ValidateUserRole_ReturnsNull_WhenRolePresent()
    {
        var controller = WithClaims(new Claim(ClaimTypes.Role, "Teacher"));
        var error = controller.ValidateUserRole(out string role);
        Assert.Null(error);
        Assert.Equal("Teacher", role);
    }

    [Fact]
    public void ValidateUserRole_ReturnsNull_WithRoleNameClaim()
    {
        var controller = WithClaims(new Claim("RoleName", "Student"));
        var error = controller.ValidateUserRole(out string role);
        Assert.Null(error);
        Assert.Equal("Student", role);
    }

    [Fact]
    public void ValidateUserRole_ReturnsUnauthorized_WhenNoRoleClaim()
    {
        var controller = WithNoClaims();
        var error = controller.ValidateUserRole(out string role);
        Assert.NotNull(error);
        Assert.IsType<UnauthorizedObjectResult>(error);
        Assert.Equal("", role);
    }
}
