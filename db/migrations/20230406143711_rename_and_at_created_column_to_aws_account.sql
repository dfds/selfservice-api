-- 2023-04-06 14:37:11 : rename and at created column to aws account

alter table "AwsAccount"
rename column "CreatedAt" to "RequestedAt";

alter table "AwsAccount"
rename column "CreatedBy" to "RequestedBy";

alter table "AwsAccount"
add column "RegisteredAt" timestamp null;
