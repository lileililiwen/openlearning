using OpenLearning.Auth.Models;
using OpenLearning.Auth.Services;
using Xunit;

namespace OpenLearning.UnitTests.Auth;

public sealed class ProfileServiceVerificationTests
{
    private static string Hash(string value)
    {
        return ProfileService.HashIdNumber(value);
    }

    [Fact]
    public void HashIdNumber_is_deterministic_and_never_plaintext()
    {
        var a = Hash("ID-12345");
        var b = Hash("ID-12345");

        Assert.Equal(a, b);
        Assert.Equal(64, a.Length);
        Assert.DoesNotContain("ID-12345", a);
    }

    [Fact]
    public void HashIdNumber_differs_for_other_inputs()
    {
        Assert.NotEqual(Hash("ID-12345"), Hash("ID-54321"));
    }
}
