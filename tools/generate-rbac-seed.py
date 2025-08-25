import csv
import uuid
from datetime import datetime

# --- Configuration ---

# Define your roles
ROLES = [
    {
        "name": "Owner",
        "existing-id": "36202DFB-D106-440D-8B99-F11BC8D77C9C",
        "description": "Full access to all resources"
        },
    {
        "name": "Contributer",
        "existing-id": "2C561A6D-90F4-4649-80B3-76A854A64EA2",
        "description": "Can modify existing resources"
    },
    {
        "name": "Reader",
        "existing-id": "22DAB91B-C2D8-4840-A173-1416EF1B882D",
        "description": "Read-only access"
    },
    {
        "name": "Guest",
        "existing-id": "F67CACC9-8DD4-4481-AC15-00B5DD83B046",
        "description": "Very limited access"
    },
]

# Define permissions per role (namespace → list of permissions)
ROLE_PERMISSIONS = {
    "Owner": {
        "topics": ["create", "read-public", "read-private", "update", "delete"],
        "capability-management": ["receive-alerts", "receive-cost", "request-deletion", "manage-permissions", "read-self-assess", "create-self-assess"],
        "capability-membership-management": ["create", "delete", "read", "read-requests", "manage-requests"],
        "tags-and-metadata": ["create", "read", "update", "delete"],
        "aws": ["create", "read", "manage-provider", "read-provider"],
        "finout": ["read-dashboards", "manage-dashboards", "manage-alerts", "read-alerts"],
        "azure": ["create", "read", "read-provider", "manage-provider"],
    },
    "Contributer": {
        "topics": ["create", "read-public", "read-private", "update", "delete"],
        "capability-management": ["receive-alerts"],
        "capability-membership-management": ["create", "read", "read-requests", "manage-requests"],
        "tags-and-metadata": ["create", "read", "update", "delete"],
        "aws": ["create", "read", "manage-provider", "read-provider"],
        "finout": ["read-dashboards", "manage-dashboards", "manage-alerts", "read-alerts"],
        "azure": ["create", "read", "read-provider", "manage-provider"],
    },
    "Reader": {
        "topics": ["read-public", "read-private"],
        "capability-membership-management": ["read", "read-requests"],
        "tags-and-metadata": ["read"],
        "aws": ["read", "read-provider"],
        "finout": ["read-dashboards", "read-alerts"],
        "azure": ["read", "read-provider"],
    },
    "Guest": {
        "topics": ["read-public"],
        "capability-membership-management": ["read"],
        "tags-and-metadata": ["read"],
        "finout": ["read-dashboards"],
    },
}

# --- Utility functions ---

def new_uuid():
    return str(uuid.uuid4())

def now_iso():
    return datetime.utcnow().isoformat()

# --- Generate RbacRole.csv ---

role_id_map = {}  # Map role name → UUID

with open("RbacRole.csv", "w", newline="") as csvfile:
    writer = csv.writer(csvfile, delimiter=";")
    writer.writerow(["Id", "OwnerId", "CreatedAt", "UpdatedAt", "Name", "Description", "Type"])

    owner_id = "0000DFD5-0000-0000-0000-00000000000A"

    for role in ROLES:
        role_id = ""
        if (role["existing-id"]):
            role_id = role["existing-id"]
        else:
            role_id = new_uuid()
        role_id_map[role["name"]] = role_id
        writer.writerow([
            role_id,
            owner_id,  # OwnerId currently unused
            now_iso(),
            now_iso(),
            role["name"],
            role["description"],
            "System",
        ])

# --- Generate RbacPermissionGrants.csv ---

with open("RbacPermissionGrants.csv", "w", newline="") as csvfile:
    writer = csv.writer(csvfile, delimiter=";")
    writer.writerow(["Id", "CreatedAt", "AssignedEntityType", "AssignedEntityId", "Namespace", "Permission", "Type", "Resource"])

    for role_name, namespaces in ROLE_PERMISSIONS.items():
        role_id = role_id_map[role_name]
        for namespace, permissions in namespaces.items():
            for perm in permissions:
                writer.writerow([
                    new_uuid(),
                    now_iso(),
                    "Role",
                    role_id,
                    namespace,
                    perm,
                    "Global",
                    ""
                ])

print("✅ CSV files generated: RbacRole.csv, RbacPermissionGrants.csv")
