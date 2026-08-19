using OpenLearning.Auth.Services;
using Xunit;

namespace OpenLearning.UnitTests.Auth;

public sealed class RoleLookupTests
{
    [Fact]
    public async Task NullClassAssignmentLookup_reports_no_assignments()
    {
        var lookup = new NullClassAssignmentLookup();

        Assert.False(await lookup.IsAssignedAsync("u1", 42));
        Assert.Empty(await lookup.ListAssignedClassIdsAsync("u1"));
    }
}
