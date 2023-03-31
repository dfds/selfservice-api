-- 2023-03-31 11:06:08 : remove role arn column from aws account

alter table "AwsAccount"
drop column "RoleArn";
