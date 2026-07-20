# Authentication, Authorization, and Access Permissions
This document describes how callers are authenticated in Self Service Universe, what they are authorized to do, and which parts are governed by RBAC versus external systems.

Self Service Universe relies on Azure AD tokens, Azure AD metadata, and internal RBAC data.

## Authentication
Self Service Universe uses Azure AD JWT validation for API authentication.

All human users authenticate by signing in with Azure AD SSO.
The service also supports app/service-principal callers (non-human callers), which are mapped to an internal caller identity from token claims.

Most controller endpoints require authentication by default.
There are currently anonymous service-catalog endpoints under /apispecs.

## Authorization
Authorization is split into:

- Global permissions (for example RBAC administration and system-level operations)
- Capability-scoped permissions (what can be done inside a specific capability)

### Global Permissions
Cloud Engineer is determined from Azure AD role claims in the token.
Cloud Engineers have elevated access to many system-level functions.

Important implementation detail:
Cloud Engineer is not a blanket bypass for all checks in all code paths. Many actions still depend on RBAC permission evaluation.

### Capability Permissions
Capability permissions are controlled by RBAC roles and permissions.
Common assignable capability roles are Owner, Contributor, and Reader.

Important implementation detail:
When a user has no explicit capability role grant, the system applies Guest permissions as an implicit fallback.
So "not a member" maps to Guest behavior by default, not Reader behavior by default.

Owner semantics include preventing the last Owner from leaving a capability.
When a capability is created through normal flows, the creator is granted Owner.

RBAC management UI:
https://ssu-preview.hellman.oxygen.dfds.cloud/admin/rbac

#### Capability Permissions for Third Party Services
Third-party services (for example AWS or Confluent Cloud) are authorized through Azure AD groups and platform integrations.
Every Capability will have an Azure AD group for the Capability members.
If you are a member of this group, you will have access to the third party services connected to the capability.

This access is separate from Self Service Universe RBAC.
In practice, access to external systems will depend on external group membership and provisioning state, not only on in-app role grants.

### Cloud Engineers and Capability Permissions
Cloud Engineers generally have elevated capabilities and broad operational access.

However, this should not be described as "always owner of all capabilities" in implementation terms.
For several operations, effective access is still granted through RBAC role/permission resolution.


## Known Issues and Operational Risks

### Azure AD Group and User Management Risk
Because access is based on Azure AD tokens and metadata, privileged identity administration in Azure AD is a critical trust boundary.
Misconfiguration or malicious role assignment in Azure AD could grant excessive access.

### Hard-Coded Accesses
There is a hard-coded allowlist for batch capability creation in addition to Cloud Engineer access.
This is operationally convenient but harder to audit and maintain than pure RBAC.

### Middleware Kill-Switch Risk
The RBAC auth-check middleware can be disabled by environment variable.
If disabled, permission-attribute checks performed by that middleware are bypassed.
This should only be used intentionally and with strong operational controls.

### Anonymous Endpoints
Service catalog endpoints under /apispecs are intentionally anonymous.
This is expected behavior but should remain explicit in threat modeling and external exposure reviews.