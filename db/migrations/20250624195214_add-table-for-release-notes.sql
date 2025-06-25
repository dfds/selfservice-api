-- 2025-06-24 19:52:14 : add-table-for-release-notes

create table "ReleaseNote" (
    "Id" uuid not null,
    "ReleaseDate"       timestamp not null,
    "Title"             varchar(255) not null,
    "Content"           text not null,
    "CreatedAt"         timestamp not null default now(),
    "CreatedBy"         varchar(255) not null,
    "ModifiedAt"         timestamp not null default now(),
    "ModifiedBy"         varchar(255) not null,
    "IsActive"          boolean not null default true,

    constraint "ReleaseNotes_PK" primary key ("Id")
);
