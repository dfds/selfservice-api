# Authentication, Authorization, and Access Permissions
This document describes how callers are authenticated in Self Service Universe, how authorization is evaluated, and where Self Service RBAC stops and external platform access begins.

Self Service Universe uses Azure AD only for authentication. Authorization is defined in Self Service's own RBAC model, using internal roles, permissions, role grants, and RBAC groups.

Authorization for usage of third party services still lies with Azure AD.

## Authentication
Self Service Universe uses Azure AD JWT validation for API authentication.

All human users authenticate by signing in with Azure AD SSO.
The service also supports app or service-principal callers, which are mapped to an internal caller identity from token claims.

Most endpoints require authentication by default.
There exists anonymous service-catalog endpoints under /apispecs.

## Authorization
Authorization is evaluated by the internal RBAC system.

The two main scopes are:

- Global permissions for system-wide and administrative operations
- Capability-scoped permissions for actions inside a specific capability

Both controller-level permission attributes and service-level authorization checks resolve through the RBAC application service. In practice, this means the source of truth for authorization is the RBAC data stored by Self Service, not Azure AD role metadata.

### RBAC Building Blocks
The RBAC model is composed of:

- Permissions such as create, read, update, delete, manage-requests, or request-deletion
- Namespaces that group permissions by domain, such as Topics, Aws, Azure, TagsAndMetadata, CapabilityMembershipManagement, and SystemAdmin
- Role grants that assign a role to a user or group for a resource
- Permission grants that assign permissions directly to a user, group, or role
- RBAC groups whose members inherit the role grants and permission grants assigned to those groups

For permission evaluation, Self Service combines:

- Direct user grants
- Grants inherited through RBAC group membership
- Permissions implied by assigned roles

### Global Permissions
Global permissions cover system-level operations such as RBAC administration and other administrative capabilities.

These permissions are evaluated in the same RBAC system as capability permissions. The relevant namespace for many of these checks is SystemAdmin, with some operations also using other global namespaces such as CapabilityManagement.

### Capability Permissions
Capability permissions are evaluated against the capability resource being accessed.

Common assignable capability roles are Owner, Contributor, and Reader. Guest exists in the RBAC model, but it is not assignable. Instead, Guest is the implicit default role used when a user has no explicit capability role for that capability.

Important implementation detail:
"No explicit membership" does not mean "no access at all". If a capability-scoped check is performed and the user has no explicit capability role grant for that capability, Guest permissions are applied implicitly.

Owner semantics still include protecting the last Owner on a capability. A user who is the last remaining Owner cannot leave that capability.

When a capability is created through the normal membership application flow, the creator is granted the Owner role for that capability.

RBAC management UI:
https://ssu-preview.hellman.oxygen.dfds.cloud/admin/rbac

#### Capability Permissions for Third Party Services
Third-party services (for example AWS or Confluent Cloud) are authorized through Azure AD groups and platform integrations.
Every Capability will have an Azure AD group for the Capability members.
If you are a member of this group, you will have access to the third party services connected to the capability.

This access is separate from Self Service Universe RBAC.
In practice, access to external systems will depend on external group membership and provisioning state, not only on in-app role grants.


### RBAC Groups
RBAC groups are part of the authorization model inside Self Service.

Users can receive effective permissions in two ways:

- Directly, through grants assigned to the user
- Indirectly, through membership in one or more RBAC groups

This replaces the earlier description where Azure AD metadata was treated as the source of role definitions. Azure AD still proves identity, but effective authorization is resolved from Self Service RBAC data.

### Access to Third-Party Services
Access inside Self Service and access in external platforms are related but not identical.

Self Service RBAC determines whether a caller can perform actions in the Self Service API and UI, for example requesting an AWS account, managing capability membership, or updating metadata.

External platform access, such as access in AWS, Confluent Cloud, or other integrated systems, still depends on the provisioning and synchronization flows for those platforms. In other words, an in-app RBAC grant is not by itself a guarantee that external access has already been provisioned.

## Important Nuances

### Middleware and Service Checks
Most endpoint checks are enforced through RBAC permission attributes in middleware, and many domain operations also perform explicit authorization checks in the service layer.

That layered approach is intentional. Authorization should be understood as a combination of middleware enforcement and domain-level checks, both backed by the same RBAC service.


## Known Issues and Operational Risks

### Azure AD Group and User Management Risk
Because third party service access access is based on Azure AD groups, privileged identity administration in Azure AD is a critical trust boundary.
Misconfiguration or malicious role assignment in Azure AD could grant excessive access.

### Middleware Kill-Switch Risk
The RBAC auth-check middleware can be disabled by environment variable.
If disabled, permission-attribute checks performed by that middleware are bypassed.
This should only be used intentionally and with strong operational controls.

### Guest Fallback Needs Careful Modeling
Because Guest permissions are applied implicitly when no explicit capability role exists, changes to the Guest permission set can affect non-members across the system.

This is useful and intentional, but it also means Guest permissions are security-sensitive and should be reviewed carefully.

### External Provisioning Drift
Self Service authorization and third-party platform authorization are not the same control plane.
If provisioning or synchronization is delayed or fails, a user may be authorized inside Self Service before equivalent access exists externally, or vice versa.

### Anonymous Endpoints
Service catalog endpoints under /apispecs are intentionally anonymous.
This is expected behavior but should remain explicit in threat modeling and external exposure reviews.