-- 2026-11-06 14:29:32 : create compliance snapshot tables

CREATE TABLE IF NOT EXISTS "ComplianceSnapshot" (
    "Id" uuid PRIMARY KEY,
    "SnapshotDate" date NOT NULL,
    "CapturedAt" timestamp with time zone NOT NULL,
    "CapabilityId" varchar(255) NOT NULL,
    "CapabilityName" text NOT NULL,
    "JsonMetadata" text NULL,
    -- NULL marks a rogue capability (no cost centre in metadata)
    "CostCentre" text NULL,
    -- 0 marks an orphaned capability
    "MemberCount" integer NOT NULL,
    "OverallStatus" varchar(32) NOT NULL,
    "Categories" jsonb NOT NULL,

    CONSTRAINT "ComplianceSnapshot_SnapshotDate_CapabilityId_UQ" UNIQUE ("SnapshotDate", "CapabilityId")
);

CREATE INDEX IF NOT EXISTS "IX_ComplianceSnapshot_CapabilityId_SnapshotDate"
    ON "ComplianceSnapshot" ("CapabilityId", "SnapshotDate");

CREATE TABLE IF NOT EXISTS "ComplianceDailySummary" (
    "SnapshotDate" date PRIMARY KEY,
    "CapturedAt" timestamp with time zone NOT NULL,
    "TotalCapabilities" integer NOT NULL,
    "FullyCompliantCapabilities" integer NOT NULL,
    "NonCompliantCapabilities" integer NOT NULL,
    "UnknownCapabilities" integer NOT NULL,
    "Requirements" jsonb NOT NULL,
    "CostCentres" jsonb NOT NULL
);
