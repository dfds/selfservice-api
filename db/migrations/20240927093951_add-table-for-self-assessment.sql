-- 2024-09-27 09:39:51 : add-table-for-self-assessment

ALTER TABLE "SelfAssessment" RENAME COLUMN "Claim" TO "ShortName";
ALTER TABLE "SelfAssessment" ADD COLUMN "OptionId" uuid not null;

create table "SelfAssessmentOption"
(
    "Id" uuid not null,
    "RequestedAt"       timestamp not null,
    "RequestedBy"       varchar(255) not null,
    "ShortName"         varchar(255) not null,
    "Description"       varchar(255) not null,
    "Active"            boolean not null,
    "DocumentationUrl" varchar(255) not null,

    constraint "SelfAssessmentOption_PK" primary key ("Id")
);
