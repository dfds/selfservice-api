-- 2023-08-24 10:54:xx : Make sure ModifiedBy is not null

UPDATE "Capability"
SET "ModifiedBy" = "CreatedBy"
WHERE "ModifiedBy" IS NULL;
