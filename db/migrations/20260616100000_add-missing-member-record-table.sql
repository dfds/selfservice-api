-- 2026-06-16 10:00:00 : add-missing-member-record-table

CREATE TABLE IF NOT EXISTS "MissingMemberRecords"
(
    "Id"              uuid      NOT NULL,
    "UserId"          varchar(255) NOT NULL,
    "Status"          varchar(50) NOT NULL, -- NotFound, Deactivated
    "FirstSeenMissingAt" timestamp NOT NULL,
    "LastCheckedAt"   timestamp NOT NULL,
    "CreatedAt"       timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt"       timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "MissingMemberRecords_PK" PRIMARY KEY ("Id"),
    CONSTRAINT "MissingMemberRecords_UserId_FK" FOREIGN KEY ("UserId") REFERENCES "Member" ("Id") ON DELETE CASCADE,
    UNIQUE ("UserId")
);

CREATE INDEX "MissingMemberRecords_FirstSeenMissingAt_idx" ON "MissingMemberRecords" ("FirstSeenMissingAt" ASC);
CREATE INDEX "MissingMemberRecords_LastCheckedAt_idx" ON "MissingMemberRecords" ("LastCheckedAt" ASC);
