using SelfService.Configuration;
using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class RbacRoleBuilder
{
    private string _name;
    private RbacAccessType _accessType;

    public RbacRoleBuilder()
    {
        _name = "Role Test";
        _accessType = RbacAccessType.Global;
    }

    public RbacRoleBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public RbacRoleBuilder WithAccessType(RbacAccessType accessType)
    {
        _accessType = accessType;
        return this;
    }

    public RbacRole Build()
    {
        return new RbacRole(
            RbacRoleId.New(),
            "some owner",
            DateTime.UtcNow,
            DateTime.UtcNow,
            _name,
            "some description",
            _accessType
        );
    }

    public static implicit operator RbacRole(RbacRoleBuilder builder) => builder.Build();
}
