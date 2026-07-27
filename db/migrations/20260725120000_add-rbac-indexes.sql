-- 2026-07-25 12:00:00 : add-rbac-indexes

CREATE INDEX IF NOT EXISTS "IX_RbacGroupMember_UserId" ON "RbacGroupMember" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_RbacPermissionGrants_AssignedEntityId_Lower" ON "RbacPermissionGrants" (lower("AssignedEntityId"));
CREATE INDEX IF NOT EXISTS "IX_RbacRoleGrants_AssignedEntityId_Lower" ON "RbacRoleGrants" (lower("AssignedEntityId"));
CREATE INDEX IF NOT EXISTS "IX_RbacRoleGrants_AssignedEntityId" ON "RbacRoleGrants" ("AssignedEntityId");
CREATE INDEX IF NOT EXISTS "IX_RbacRole_Name_Lower" ON "RbacRole" (lower("Name"));
