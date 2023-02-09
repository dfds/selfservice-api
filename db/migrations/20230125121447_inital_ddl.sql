-- 2023-01-25 12:14:47 : inital ddl

create table "AwsAccount"
(
    "Id" uuid not null,
    "AccountId" varchar(12) not null,
    "RoleArn" varchar(255) not null,
    "RoleEmail" varchar(255) not null,
    "CreatedAt" timestamp not null,
    "CreatedBy" varchar(255) not null,

    constraint "AwsAccount_PK" primary key ("Id")
);

create table "Capability"
(
    "Id" varchar(255) not null,
    "Name" varchar(255) not null,
    "Description" varchar(255) not null,
    "Deleted" timestamp,
    "AwsAccountId" uuid,
    "CreatedAt" timestamp not null,
    "CreatedBy" varchar(255) not null,
    
    constraint "Capability_PK" primary key ("Id"),
    constraint "Capability_AwsAccount_Id_FK" foreign key ("AwsAccountId") references "AwsAccount" ("Id") on delete cascade
);

create table "Member"
(
    "UPN" varchar(255) not null,
    "Email" varchar(255) not null,

    constraint "Member_PK" primary key ("UPN")
);

create table "Membership"
(
    "CapabilityId" varchar(255) not null,
    "UPN" varchar(255) not null,
    "CreatedAt" timestamp not null,

    constraint "Membership_PK" primary key ("CapabilityId", "UPN"),
    constraint "Membership_Capability_Id_FK" foreign key ("CapabilityId") references "Capability" ("Id") on delete cascade,
    constraint "Membership_Member_UPN_FK" foreign key ("UPN") references "Member" ("UPN") on delete cascade
);

create table "KafkaCluster"
(
    "Id" uuid not null,
    "ClusterId" varchar(255) not null,
    "Name" varchar(1024) not null,
    "Description" varchar(8192),
    "Enabled" boolean not null,

    constraint "KafkaCluster_PK" primary key ("Id")
);

create table "KafkaTopic"
(
    "Id" uuid not null,
    "CapabilityId" varchar(255) not null,
    "KafkaClusterId" uuid not null,
    "Name" varchar(255)  not null,
    "Description" varchar(1024) not null,
    "Partitions" int not null,
    "Retention" bigint not null,
    "Status" varchar(50) not null,
    "CreatedAt" timestamp not null,
    "CreatedBy" varchar(255) not null,
    "ModifiedAt" timestamp null,
    "ModifiedBy" varchar(255) null,

    constraint "KafkaTopic_PK" primary key ("Id"),
    constraint "KafkaTopic_Capability_Id_FK" foreign key ("CapabilityId") references "Capability" ("Id") on delete cascade,
    constraint "KafkaTopic_KafkaCluster_Id_FK" foreign key ("KafkaClusterId") references "KafkaCluster" ("Id") on delete cascade,
    unique ("CapabilityId", "KafkaClusterId", "Name")
);


CREATE TABLE "ServiceCatalog" (
    "Id" uuid NOT NULL,
    "Name" varchar(255) NOT NULL,
    "Namespace" varchar(255) NOT NULL,
    "Spec" text NOT NULL,
    "CreatedAt" timestamp NOT NULL,

    CONSTRAINT "ServiceCatalog_PK" PRIMARY KEY("Id")
);
