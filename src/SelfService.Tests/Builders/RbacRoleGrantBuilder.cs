using Confluent.Kafka;
using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class RbacRoleGrantBuilder
{
    private RbacRoleId _roleId;
    private DateTime _createdAt;
    private AssignedEntityType _assignedEntityType;
    private string _assignedEntityId;
    private RbacAccessType _type;
    private string _resource;

    public RbacRoleGrantBuilder()
    {
        _roleId = RbacRoleId.New();
        _createdAt = DateTime.UtcNow;
        _assignedEntityType = AssignedEntityType.User;
        _assignedEntityId = "user-123";
        _type = RbacAccessType.Global;
        _resource = "resource-xyz";
    }

    public RbacRoleGrantBuilder WithRoleId(RbacRoleId roleId)
    {
        _roleId = roleId;
        return this;
    }

    public RbacRoleGrantBuilder AssignToUser(string userId)
    {
        _assignedEntityType = AssignedEntityType.User;
        _assignedEntityId = userId;
        return this;
    }

    public RbacRoleGrantBuilder AssignedForCapability(string capabilityId)
    {
        _type = RbacAccessType.Capability;
        _resource = capabilityId;
        return this;
    }

    public RbacRoleGrant Build()
    {
        return new RbacRoleGrant(
            RbacRoleGrantId.New(),
            _roleId,
            _createdAt,
            _assignedEntityType,
            _assignedEntityId,
            _type,
            _resource
        );
    }

    public static implicit operator RbacRoleGrant(RbacRoleGrantBuilder builder) => builder.Build();
}
