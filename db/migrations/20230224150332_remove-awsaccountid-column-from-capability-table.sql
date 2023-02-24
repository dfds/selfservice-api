-- 2023-02-24 15:03:32 : remove-awsaccountid-column-from-capability-table

ALTER TABLE "Capability"
    DROP COLUMN "AwsAccountId";
