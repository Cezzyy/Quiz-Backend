using OnlineQuiz.Utilities;
using Xunit;

namespace OnlineQuiz.Tests.Utilities;

public class PasswordHasherTests
{
    private const string Password = "CorrectHorseBatteryStaple";
    private static readonly string PrecomputedHash = BCrypt.Net.BCrypt.HashPassword(Password, 4);

    [Fact]
    public void HashPassword_ProducesDifferentValueThanInput()
    {
        var password = "MySecureP@ssw0rd!";

        var hash = BCrypt.Net.BCrypt.HashPassword(password, 4);

        Assert.NotNull(hash);
        Assert.NotEqual(password, hash);
        Assert.True(hash.StartsWith("$2b$", StringComparison.Ordinal) || hash.StartsWith("$2a$", StringComparison.Ordinal));
    }

    [Fact]
    public void VerifyPassword_ReturnsTrue_ForCorrectPassword()
    {
        var password = Password;
        var hash = PrecomputedHash;

        var result = PasswordHasher.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_ForIncorrectPassword()
    {
        var wrongPassword = "Tr0ub4dor&3";
        var hash = PrecomputedHash;

        var result = PasswordHasher.VerifyPassword(wrongPassword, hash);

        Assert.False(result);
    }
}

