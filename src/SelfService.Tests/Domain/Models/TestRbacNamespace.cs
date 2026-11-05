using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestRbacNamespace
{
    [Theory]
    [InlineData("copilot")]
    [InlineData("Copilot")]
    public void parses_copilot(string input)
    {
        Assert.True(RbacNamespace.TryParse(input, out var parsed));
        Assert.Equal(RbacNamespace.Copilot, parsed);
        Assert.Equal("copilot", parsed.ToString());
    }

    [Fact]
    public void copilot_permissions_are_global()
    {
        var copilot = Permission
            .BootstrapPermissions()
            .Where(p => p.Namespace == RbacNamespace.Copilot)
            .ToDictionary(p => p.Name, p => p.AccessType);

        Assert.Equal(2, copilot.Count);
        Assert.Equal(RbacAccessType.Global, copilot["read-own"]);
        Assert.Equal(RbacAccessType.Global, copilot["read-all"]);
    }
}
