-- 2026-11-11 12:00:00 : add-rbac-grantable-role

-- A holder of "RoleId" on a capability may grant "GrantableRoleId" on that capability.
CREATE TABLE IF NOT EXISTS "RbacGrantableRole"
(
    "RoleId"          uuid      not null,
    "GrantableRoleId" uuid      not null,
    "CreatedAt"       timestamp not null default current_timestamp,

    constraint "RbacGrantableRole_PK" primary key ("RoleId", "GrantableRoleId"),
    constraint "RbacGrantableRole_RoleId_FK" foreign key ("RoleId") references "RbacRole" ("Id") on delete cascade,
    constraint "RbacGrantableRole_GrantableRoleId_FK" foreign key ("GrantableRoleId") references "RbacRole" ("Id") on delete cascade
);

-- Initial rules: a role may grant the roles whose permissions it holds. Matched by name because role
-- ids differ between environments; inserts nothing where the roles do not exist yet (local seeding
-- runs after migrations and loads db/seed/RbacGrantableRole.csv instead).
INSERT INTO "RbacGrantableRole" ("RoleId", "GrantableRoleId")
SELECT granter."Id", grantable."Id"
FROM (VALUES
          ('owner', 'owner'),
          ('owner', 'contributor'),
          ('owner', 'reader'),
          ('contributor', 'contributor'),
          ('contributor', 'reader')
     ) AS rule (granter_name, grantable_name)
JOIN "RbacRole" granter ON lower(granter."Name") = rule.granter_name
JOIN "RbacRole" grantable ON lower(grantable."Name") = rule.grantable_name
ON CONFLICT DO NOTHING;
