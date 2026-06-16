-- 2026-06-15 12:00:00 : add-favourite-table

create table "Favourite"
(
    "Id" uuid not null,
    "CapabilityId" varchar(255) not null,
    "UserId" varchar(255) not null,
    "CreatedAt" timestamp not null,

    constraint "Favourite_PK" primary key ("Id"),
    constraint "Favourite_CapabilityId_UserId_UQ" unique ("CapabilityId", "UserId")
);

create index "Favourite_UserId_IDX"
    on "Favourite" ("UserId");
