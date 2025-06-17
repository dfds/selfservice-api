-- noinspection SqlNoDataSourceInspectionForFile

-- 2025-06-16 12:58:00 : add-rbac-schemas

CREATE TABLE IF NOT EXISTS "RbacPermissionGrants"
(
    "Id"                 uuid         not null,
    "CreatedAt"          timestamp    not null default current_timestamp,
    "AssignedEntityType" varchar(255) not null, -- user,group
    "AssignedEntityId"   varchar(255) not null,
    "Namespace"          text         not null,
    "Permission"         text         not null,
    "Type"               varchar(255) not null, -- global,capability
    "Resource"           text,

    constraint "RbacPermissionGrants_PK" primary key ("Id")
);

CREATE TABLE IF NOT EXISTS "RbacRoleGrants"
(
    "Id"                 uuid         not null,
    "CreatedAt"          timestamp    not null default current_timestamp,
    "AssignedEntityType" varchar(255) not null, -- user,group
    "AssignedEntityId"   varchar(255) not null,
    "Name"               text         not null,
    "Type"               varchar(255) not null, -- global,capability
    "Resource"           text,

    constraint "RbacRoleGrants_PK" primary key ("Id")
);
