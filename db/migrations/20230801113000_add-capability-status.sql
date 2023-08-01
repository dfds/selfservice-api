-- 2023-08-01 11:30:xx : Add status and modified_at to capabilities

-- add columns
ALTER TABLE "Capability"
ADD column "Status" varchar(255) NOT NULL DEFAULT 'Active',
ADD column "ModifiedAt" timestamp NOT NULL DEFAULT clock_timestamp();

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

-- remove deleted coloumn
ALTER TABLE "Capability"
DROP column "Deleted";
