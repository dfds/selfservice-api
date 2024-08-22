-- 4Y-07-15 11:07:55 : add-table-for-capability-claims

create table "CapabilityClaim"
(
    "Id" uuid not null,
    "RequestedAt" timestamp not null,
    "RequestedBy" varchar(255) not null,
    "CapabilityId" varchar(255) not null,
    "Claim" varchar(255) not null,

    constraint "CapabilityClaim_PK" primary key ("Id")
);
