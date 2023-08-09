-- 2023-08-01 11:30:xx : Add status and modified_at to capabilities

ALTER TABLE "Capability"
ADD COLUMN "Status" VARCHAR(255) NOT NULL DEFAULT 'Active',
ADD COLUMN "ModifiedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP; -- always overwritten, but non-nulls should have a default

-- update status and modified_at to fit existing deletions
-- also update modified_at to fit existing created_at
UPDATE "Capability"
SET
    "Status" = 'Deleted',
    "ModifiedAt" = "Deleted"
WHERE "Deleted" IS NOT NULL;

UPDATE "Capability"
SET "ModifiedAt" = "CreatedAt"
WHERE "Deleted" IS NULL;

ALTER TABLE "Capability"
DROP COLUMN "Deleted";
