create table "EmailBroadcast"
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
    "CreatedAt"         timestamp not null,
    "CreatedBy"         text not null,
    "ModifiedAt"        timestamp not null,
    "ModifiedBy"        text not null,
    "SentAt"            timestamp with time zone,
    "CancelledAt"       timestamp with time zone,

    constraint "EmailBroadcast_PK" primary key ("Id")
);

create table "EmailBroadcastRecipientLog"
(
    "Id"                  uuid not null,
    "EmailBroadcastId"    uuid not null,
    "CapabilityId"        text not null,
    "CapabilityName"      text not null,
    "UserId"              text not null,
    "Email"               text not null,
    "RenderedSubject"     text not null,
    "RenderedHtml"        text not null,
    "Status"              text not null default 'Pending',
    "SentAt"              timestamp with time zone,
    "ErrorMessage"        text,
    "CreatedAt"           timestamp not null,

    constraint "EmailBroadcastRecipientLog_PK" primary key ("Id"),
    constraint "EmailBroadcastRecipientLog_Broadcast_FK" foreign key ("EmailBroadcastId") references "EmailBroadcast"("Id") on delete cascade
);

create index "IX_EmailBroadcastRecipientLog_BroadcastId" on "EmailBroadcastRecipientLog" ("EmailBroadcastId");
