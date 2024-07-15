create table "CapabilityXaxa"
(
    "Id" uuid not null,
    "RequestedAt" timestamp not null,
    "RequestedBy" varchar(255) not null,
    "CapabilityId" varchar(255) not null,
    "Environment" varchar(255) not null,

    constraint "CapabilityXaxa_PK" primary key ("Id")
);
