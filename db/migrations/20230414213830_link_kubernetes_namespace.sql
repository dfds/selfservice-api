-- 2023-04-14 21:38:30 : link kubernetes namespace

alter table "AwsAccount"
rename column "CompletedAt" to "LinkedAt";

alter table "AwsAccount"
add column "Namespace" varchar(255) null;
