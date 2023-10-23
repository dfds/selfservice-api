-- 2023-10-13 13:02:03 : create team capability linking table

CREATE TABLE "TeamCapabilityLinking"
(
    "Id"           UUID         NOT NULL,
    "TeamId"       UUID         NOT NULL,
    "CapabilityId" VARCHAR(255) NOT NULL,
    "CreatedBy"    VARCHAR(255) NOT NULL,
    "CreatedAt"    TIMESTAMP    NOT NULL,

    CONSTRAINT "TeamCapabilityLinking_PK" PRIMARY KEY ("Id"),
    CONSTRAINT "TeamCapabilityLinking_TeamId_FK" FOREIGN KEY ("TeamId") REFERENCES "Team" ("Id") ON DELETE CASCADE,
    CONSTRAINT "TeamCapabilityLinking_CapabilityId_FK" FOREIGN KEY ("CapabilityId") REFERENCES "Capability" ("Id") ON DELETE CASCADE
);