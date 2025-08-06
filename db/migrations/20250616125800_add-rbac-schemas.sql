-- noinspection SqlNoDataSourceInspectionForFile

-- 2025-06-16 12:58:00 : add-rbac-schemas

CREATE TABLE IF NOT EXISTS "RbacPermissionGrants"
(
    "Id"                 uuid         not null,
    "CreatedAt"          timestamp    not null default current_timestamp,
    "AssignedEntityType" varchar(255) not null, -- user,group,role
    "AssignedEntityId"   varchar(255) not null,
    "Namespace"          text         not null,
    "Permission"         text         not null,
    "Type"               varchar(255) not null, -- global,capability
    "Resource"           text,

    constraint "RbacPermissionGrants_PK" primary key ("Id"),
    unique ("AssignedEntityType", "AssignedEntityId", "Namespace", "Permission", "Type", "Resource")
);

CREATE TABLE IF NOT EXISTS "RbacRole"
(
    "Id"          uuid      not null,
    "OwnerId"     text      not null, -- depends on type, can be uuid, rootid
    "CreatedAt"   timestamp not null default current_timestamp,
    "UpdatedAt"   timestamp not null default current_timestamp,
    "Name"        text      not null,
    "Description" text      not null,
    "Type"        text      not null, -- system,capability,group

    constraint "RbacRole_PK" primary key ("Id")
);


CREATE TABLE IF NOT EXISTS "RbacRoleGrants"
(
    "Id"                 uuid         not null,
    "RoleId"             uuid         not null,
    "CreatedAt"          timestamp    not null default current_timestamp,
    "AssignedEntityType" varchar(255) not null, -- user,group
    "AssignedEntityId"   varchar(255) not null,
    "Type"               varchar(255) not null, -- global,capability
    "Resource"           text,  

    constraint "RbacRoleGrants_PK" primary key ("Id"),
    constraint "RbacRoleGrants_RoleId_FK" foreign key ("RoleId") references "RbacRole" ("Id") on delete cascade
);

CREATE TABLE IF NOT EXISTS "RbacGroup"
(
    "Id"          uuid      not null,
    "CreatedAt"   timestamp not null default current_timestamp,
    "UpdatedAt"   timestamp not null default current_timestamp,
    "Name"        text      not null,
    "Description" text      not null,

    constraint "RbacGroup_PK" primary key ("Id")
);

CREATE TABLE IF NOT EXISTS "RbacGroupMember"
(
    "Id"        uuid         not null,
    "GroupId"   uuid         not null,
    "UserId"    varchar(255) not null,
    "CreatedAt" timestamp    not null default current_timestamp,

    UNIQUE ("GroupId", "UserId"),

    constraint "RbacGroupMember_PK" primary key ("Id"),
    constraint "RbacGroup_UserId_FK" foreign key ("UserId") references "Member" ("Id") on delete cascade,
    constraint "RbacGroup_GroupId_FK" foreign key ("GroupId") references "RbacGroup" ("Id") on delete cascade
);