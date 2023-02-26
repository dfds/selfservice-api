-- 2023-02-27 09:12:24 : add-table-for-membershipapplication

create table "MembershipApplication"
(
    "Id" uuid not null,
    "CapabilityId" varchar(255) not null,
    "Applicant" varchar(255) not null,
    "Status" varchar(255) not null,

    "SubmittedAt" timestamp not null,
    "ExpiresOn" timestamp not null,

    constraint "MembershipApplication_PK" primary key ("Id")
);
