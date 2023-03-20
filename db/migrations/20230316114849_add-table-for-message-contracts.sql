-- 2023-03-16 11:48:49 : add-table-for-message-contracts

create table "MessageContract"
(
    "Id" uuid not null,
    "KafkaTopicId" uuid not null,
    "MessageType" varchar(255) not null,
    "Description" varchar(1024) null,
    "Example" text not null,
    "Schema" text not null,
    "Status" varchar(50) not null,
    "CreatedAt" timestamp not null,
    "CreatedBy" varchar(255) not null,
    "ModifiedAt" timestamp null,
    "ModifiedBy" varchar(255) null,

    constraint "MessageContract_PK" primary key ("Id")
);
