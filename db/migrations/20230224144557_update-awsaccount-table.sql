-- 2023-02-24 14:45:57 : update-awsaccount-table

ALTER TABLE "AwsAccount"
	ADD COLUMN "CapabilityId" varchar(255);

UPDATE "AwsAccount" aa SET "CapabilityId" = c."Id" 
FROM "Capability" c WHERE c."AwsAccountId" = aa."Id"; 

ALTER TABLE "AwsAccount"
	ALTER COLUMN "CapabilityId" SET NOT NULL;
