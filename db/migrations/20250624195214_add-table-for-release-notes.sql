-- noinspection SqlNoDataSourceInspectionForFile

-- 2025-06-24 19:52:14 : add-table-for-release-notes

create table "ReleaseNote"
(
    "Id"          uuid         not null,
    "ReleaseDate" timestamp    not null,
    "Title"       varchar(255) not null,
    "Content"     text         not null,
    "CreatedAt"   timestamp    not null default now(),
    "CreatedBy"   varchar(255) not null,
    "ModifiedAt"  timestamp    not null default now(),
    "ModifiedBy"  varchar(255) not null,
    "IsActive"    boolean      not null default true,
    "Version"     BIGINT       not null default 1,

    constraint "ReleaseNotes_PK" primary key ("Id")
);

create table "ReleaseNoteHistory"
(
    "Id"            uuid         not null,
    "ReleaseNoteId" uuid         not null REFERENCES "ReleaseNote"("Id") ON DELETE CASCADE,
    "ReleaseDate"   timestamp    not null,
    "Title"         varchar(255) not null,
    "Content"       text         not null,
    "CreatedAt"     timestamp    not null default now(),
    "CreatedBy"     varchar(255) not null,
    "ModifiedAt"    timestamp    not null default now(),
    "ModifiedBy"    varchar(255) not null,
    "IsActive"      boolean      not null default true,
    "Version"       BIGINT       not null default 1,

    constraint "ReleaseNotesHistory_PK" primary key ("Id")
);
