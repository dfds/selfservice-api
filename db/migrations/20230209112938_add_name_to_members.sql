-- 2023-02-09 11:29:38 : add name to members

alter table "Member"
    add column "DisplayName" varchar(255);
