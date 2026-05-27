create table "EmailCampaign"
(
    "Id"                uuid not null,
    "Name"              text not null,
    "Subject"           text not null,
    "ContentJson"       text not null,
    "ContentHtml"       text,
    "AudienceJson"      jsonb not null default '{"mode":"all"}',
    "RecipientFilter"   text,
    "ScheduleType"      text not null default 'Immediate',
    "ScheduledAt"       timestamp with time zone,
    "CronExpression"    text,
    "Status"            text not null default 'Draft',
    "IsDeleted"         boolean not null default false,
    "CreatedAt"         timestamp not null,
    "CreatedBy"         text not null,
    "ModifiedAt"        timestamp not null,
    "ModifiedBy"        text not null,
    "SentAt"            timestamp with time zone,
    "CancelledAt"       timestamp with time zone,

    constraint "EmailCampaign_PK" primary key ("Id")
);

create index "IX_EmailCampaign_IsDeleted" on "EmailCampaign" ("IsDeleted") where "IsDeleted" = false;

create table "EmailCampaignExecution"
(
    "Id"                uuid not null,
    "EmailCampaignId"   uuid not null references "EmailCampaign"("Id") on delete cascade,
    "ExecutedAt"        timestamp with time zone not null,
    "TotalRecipients"   int not null,
    "SuccessCount"      int not null default 0,
    "FailureCount"      int not null default 0,
    "Status"            text not null,

    constraint "EmailCampaignExecution_PK" primary key ("Id")
);

create index "IX_EmailCampaignExecution_CampaignId" on "EmailCampaignExecution" ("EmailCampaignId");

create table "EmailCampaignRecipientLog"
(
    "Id"                uuid not null,
    "EmailCampaignId"   uuid not null,
    "ExecutionId"       uuid null references "EmailCampaignExecution"("Id") on delete set null,
    "CapabilityId"      text not null,
    "CapabilityName"    text not null,
    "UserId"            text not null,
    "Email"             text not null,
    "RenderedSubject"   text not null,
    "RenderedHtml"      text not null,
    "Status"            text not null default 'Pending',
    "SentAt"            timestamp with time zone,
    "ErrorMessage"      text,
    "CreatedAt"         timestamp not null,

    constraint "EmailCampaignRecipientLog_PK" primary key ("Id"),
    constraint "EmailCampaignRecipientLog_Campaign_FK" foreign key ("EmailCampaignId") references "EmailCampaign"("Id") on delete cascade
);

create index "IX_EmailCampaignRecipientLog_CampaignId" on "EmailCampaignRecipientLog" ("EmailCampaignId");
create index "IX_EmailCampaignRecipientLog_ExecutionId" on "EmailCampaignRecipientLog" ("ExecutionId") where "ExecutionId" is not null;
