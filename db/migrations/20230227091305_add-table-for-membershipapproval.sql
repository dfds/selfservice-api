-- 2023-02-27 09:13:05 : add-table-for-membershipapproval

create table "MembershipApproval"
(
    "Id" uuid not null,
    "ApprovedBy" varchar(255) not null,
    "ApprovedAt" timestamp not null,
    "MembershipApplicationId" uuid not null,

    constraint "MembershipApproval_PK" primary key ("Id")
);
