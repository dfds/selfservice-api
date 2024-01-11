-- 2024-01-10 10:22:10 : add schema version column
ALTER TABLE "MessageContract" ADD COLUMN "SchemaVersion" INT NOT NULL DEFAULT 1;