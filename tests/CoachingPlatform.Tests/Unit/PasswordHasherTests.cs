using CoachingPlatform.Application.Common;
using Xunit;

namespace CoachingPlatform.Tests.Unit;

public class PasswordHasherTests
{
    private readonly IPasswordHasher _hasher = new PasswordHasher();

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Password123!");
        Assert.True(_hasher.Verify("Password123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Password123!");
        Assert.False(_hasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_UsesUniqueSalt_PerCall()
        => Assert.NotEqual(_hasher.Hash("same"), _hasher.Hash("same"));

    [Fact]
    public void Verify_MalformedHash_ReturnsFalse()
        => Assert.False(_hasher.Verify("anything", "not-a-hash"));
}
