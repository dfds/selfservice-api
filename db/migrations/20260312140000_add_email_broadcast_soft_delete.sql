alter table "EmailBroadcast" add column "IsDeleted" boolean not null default false;

create index "IX_EmailBroadcast_IsDeleted" on "EmailBroadcast" ("IsDeleted") where "IsDeleted" = false;
