-- 2023-10-13 13:02:03 : create team capability association table

CREATE TABLE "TeamCapabilityAssociation"
(
    "Id"           UUID         NOT NULL,
    "TeamId"       UUID         NOT NULL,
    "CapabilityId" VARCHAR(255) NOT NULL,
    "CreatedBy"    VARCHAR(255) NOT NULL,
    "CreatedAt"    TIMESTAMP    NOT NULL,

    CONSTRAINT "TeamCapabilityAssociation_PK" PRIMARY KEY ("Id"),
    CONSTRAINT "TeamCapabilityAssociation_TeamId_FK" FOREIGN KEY ("TeamId") REFERENCES "Team" ("Id") ON DELETE CASCADE,
    CONSTRAINT "TeamCapabilityAssociation_CapabilityId_FK" FOREIGN KEY ("CapabilityId") REFERENCES "Capability" ("Id") ON DELETE CASCADE
)