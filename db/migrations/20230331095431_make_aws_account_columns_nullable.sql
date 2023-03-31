-- 2023-03-31 09:54:31 : make aws account columns nullable

alter table "AwsAccount"
alter column "AccountId" drop not null,
alter column "RoleArn" drop not null,
alter column "RoleEmail" drop not null;
