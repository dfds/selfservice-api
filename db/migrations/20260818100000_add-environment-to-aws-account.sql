-- Add Environment column to AwsAccount table
ALTER TABLE "AwsAccount" ADD COLUMN "Environment" varchar(255);

-- Backfill existing rows with 'prod' as default environment
UPDATE "AwsAccount" SET "Environment" = 'prod' WHERE "Environment" IS NULL;

-- Make Environment NOT NULL
ALTER TABLE "AwsAccount" ALTER COLUMN "Environment" SET NOT NULL;

-- Add unique index on (CapabilityId, Environment)
CREATE UNIQUE INDEX "IX_AwsAccount_CapabilityId_Environment" ON "AwsAccount" ("CapabilityId", "Environment");
