-- 2023-04-06 22:09:29 : add completed at column to aws account

alter table "AwsAccount"
add column "CompletedAt" timestamp null;
