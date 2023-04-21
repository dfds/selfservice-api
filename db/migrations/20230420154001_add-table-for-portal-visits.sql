-- 2023-04-20 15:40:01 : add-table-for-portal-visits

create table "PortalVisit"
(
    "Id" uuid not null,
    "VisitedBy" varchar(255) not null,
    "VisitedAt" timestamp not null,

    constraint "PortalVisit_PK" primary key ("Id")
);
