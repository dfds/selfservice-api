-- 2026-10-19 12:00:00 : Add private asset metadata table

CREATE TABLE IF NOT EXISTS "PrivateAsset" (
    "Id" uuid PRIMARY KEY,
    "StorageKey" varchar(1024) NOT NULL,
    "FileName" varchar(255) NOT NULL,
    "MimeType" varchar(255) NOT NULL,
    "SizeInBytes" bigint NOT NULL,
    "CreatedBy" varchar(255) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "OwnerId" uuid NOT NULL,
    "OwnerType" varchar(64) NOT NULL,
    "ContentId" varchar(255) NOT NULL,
    "IsInline" boolean NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_PrivateAsset_CreatedAt"
    ON "PrivateAsset" ("CreatedAt");

CREATE INDEX IF NOT EXISTS "IX_PrivateAsset_CreatedBy"
    ON "PrivateAsset" ("CreatedBy");

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PrivateAsset_OwnerType_OwnerId_ContentId"
    ON "PrivateAsset" ("OwnerType", "OwnerId", "ContentId");
