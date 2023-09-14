-- 2023-09-14 13:30:33 : add metadata to capability table
ALTER TABLE "Capability" ADD COLUMN "JsonMetadata" JSONB DEFAULT '{}' NOT NULL;
ALTER TABLE "Capability" ADD COLUMN "JsonMetadataSchemaVersion" INTEGER DEFAULT 0 NOT NULL;