-- 2025-10-15 09:17:26 : demo-recording
create table "DemoRecording"
(
    "Id"            uuid         not null,
    "RecordingDate" timestamp    not null,
    "Title"         varchar(255) not null,
    "Description"   varchar(255) not null,
    "Tags"          text         not null,
    "Url"           varchar(511) not null,
    "CreatedAt"     timestamp    not null default now(),
    "CreatedBy"     varchar(255) not null,
    "IsActive"      boolean      not null default true,

    constraint "DemoRecording_PK" primary key ("Id")
);
