import csv
import json
import uuid
from datetime import datetime
from pathlib import Path


CONFIG_PATH = Path(__file__).with_name("config.json")
OWNER_ID = "0000DFD5-0000-0000-0000-00000000000A"
SEED_NAMESPACE = uuid.UUID("bda25e7c-1124-4fca-9f7e-70e28d1901da")


def new_uuid():
    return str(uuid.uuid4()).upper()


def now_iso():
    return datetime.utcnow().isoformat()


def stable_uuid(kind, name):
    value = uuid.uuid5(SEED_NAMESPACE, f"{kind}:{name.strip().lower()}")
    return str(value).upper()


def load_config():
    with open(CONFIG_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


def resolve_role_id(role):
    # Support both current and legacy property names.
    existing = role.get("existingId") or role.get("existing-id")
    if existing:
        return existing
    return stable_uuid("role", role.get("name", ""))


def resolve_group_id(group):
    # Support both current and legacy property names.
    existing = group.get("existingId") or group.get("existing-id")
    if existing:
        return existing
    return stable_uuid("group", group.get("name", ""))


def create_role_id_map(roles):
    role_id_map = {}
    for role in roles:
        name = role.get("name")
        if not name:
            raise ValueError("Role entry is missing 'name'")
        if name in role_id_map:
            raise ValueError(f"Duplicate role name in config: {name}")
        role_id_map[name] = resolve_role_id(role)
    return role_id_map


def create_group_id_map(groups):
    group_id_map = {}
    for group in groups:
        name = group.get("name")
        if not name:
            raise ValueError("Group entry is missing 'name'")
        if name in group_id_map:
            raise ValueError(f"Duplicate group name in config: {name}")
        group_id_map[name] = resolve_group_id(group)
    return group_id_map


def write_roles_csv(roles, role_id_map):
    with open("RbacRole.csv", "w", newline="", encoding="utf-8") as csvfile:
        writer = csv.writer(csvfile, delimiter=";")
        writer.writerow(["Id", "OwnerId", "CreatedAt", "UpdatedAt", "Name", "Description", "Type"])

        for role in roles:
            name = role["name"]
            writer.writerow(
                [
                    role_id_map[name],
                    OWNER_ID,
                    now_iso(),
                    now_iso(),
                    name,
                    role.get("description") or f"Role: {name}",
                    role.get("type") or "Global",
                ]
            )


def write_permission_grants_csv(roles, role_id_map):
    with open("RbacPermissionGrants.csv", "w", newline="", encoding="utf-8") as csvfile:
        writer = csv.writer(csvfile, delimiter=";")
        writer.writerow(
            [
                "Id",
                "CreatedAt",
                "AssignedEntityType",
                "AssignedEntityId",
                "Namespace",
                "Permission",
                "Type",
                "Resource",
            ]
        )

        for role in roles:
            role_name = role["name"]
            role_id = role_id_map[role_name]
            permissions_by_namespace = role.get("permissions", {})
            for namespace, permissions in permissions_by_namespace.items():
                for permission in permissions:
                    writer.writerow(
                        [
                            new_uuid(),
                            now_iso(),
                            "Role",
                            role_id,
                            namespace,
                            permission,
                            "Global",
                            "",
                        ]
                    )


def write_groups_csv(groups, group_id_map):
    with open("RbacGroup.csv", "w", newline="", encoding="utf-8") as csvfile:
        writer = csv.writer(csvfile, delimiter=";")
        writer.writerow(["Id", "CreatedAt", "UpdatedAt", "Name", "Description"])

        for group in groups:
            name = group["name"]
            writer.writerow(
                [
                    group_id_map[name],
                    now_iso(),
                    now_iso(),
                    name,
                    group.get("description") or f"Group: {name}",
                ]
            )


def write_group_members_csv(groups, group_id_map):
    with open("RbacGroupMember.csv", "w", newline="", encoding="utf-8") as csvfile:
        writer = csv.writer(csvfile, delimiter=";")
        writer.writerow(["Id", "GroupId", "UserId", "CreatedAt"])

        seen = set()
        for group in groups:
            group_id = group_id_map[group["name"]]
            for member in group.get("members", []):
                key = (group_id, member.lower())
                if key in seen:
                    continue
                seen.add(key)
                writer.writerow([new_uuid(), group_id, member, now_iso()])


def append_group_role_grant(grants, role_id_map, group_id, role_name, grant_type="Global", resource=""):
    role_id = role_id_map.get(role_name)
    if not role_id:
        raise ValueError(f"Role '{role_name}' referenced by grants was not found in config.roles")

    grants.append(
        {
            "RoleId": role_id,
            "AssignedEntityType": "Group",
            "AssignedEntityId": group_id,
            "Type": grant_type,
            "Resource": resource,
        }
    )


def build_role_grants(groups, role_id_map, group_id_map):
    grants = []

    for group in groups:
        group_id = group_id_map[group["name"]]
        for binding in group.get("roles", []):
            role_name = binding.get("roleName")
            if not role_name:
                continue

            grant_type = binding.get("scope") or "Global"
            resource = binding.get("resource") or ""
            append_group_role_grant(grants, role_id_map, group_id, role_name, grant_type, resource)

    # Remove accidental duplicates while preserving first occurrence order.
    deduped = []
    seen = set()
    for grant in grants:
        key = (
            grant["RoleId"],
            grant["AssignedEntityType"],
            grant["AssignedEntityId"],
            grant["Type"],
            grant["Resource"],
        )
        if key in seen:
            continue
        seen.add(key)
        deduped.append(grant)

    return deduped


def write_role_grants_csv(grants):
    with open("RbacRoleGrants.csv", "w", newline="", encoding="utf-8") as csvfile:
        writer = csv.writer(csvfile, delimiter=";")
        writer.writerow(["Id", "RoleId", "CreatedAt", "AssignedEntityType", "AssignedEntityId", "Type", "Resource"])

        for grant in grants:
            writer.writerow(
                [
                    new_uuid(),
                    grant["RoleId"],
                    now_iso(),
                    grant["AssignedEntityType"],
                    grant["AssignedEntityId"],
                    grant["Type"],
                    grant["Resource"],
                ]
            )


def main():
    config = load_config()
    roles = config.get("roles", [])
    if not roles:
        raise ValueError("No roles found in config.json")

    groups = config.get("groups", [])

    role_id_map = create_role_id_map(roles)
    group_id_map = create_group_id_map(groups)

    write_roles_csv(roles, role_id_map)
    write_permission_grants_csv(roles, role_id_map)
    write_groups_csv(groups, group_id_map)
    write_group_members_csv(groups, group_id_map)

    role_grants = build_role_grants(groups, role_id_map, group_id_map)
    write_role_grants_csv(role_grants)

    print(
        "✅ CSV files generated: RbacRole.csv, RbacGroup.csv, RbacGroupMember.csv, "
        "RbacPermissionGrants.csv, RbacRoleGrants.csv"
    )


if __name__ == "__main__":
    main()
