-- 2024-09-27 09:39:51 : add-table-for-self-assessment

create table "CapabilityClaim"
(
    "Id" uuid not null,
    "RequestedAt" timestamp not null,
    "RequestedBy" varchar(255) not null,
    "CapabilityId" varchar(255) not null,
    "Claim" varchar(255) not null,

    constraint "CapabilityClaim_PK" primary key ("Id")
);
