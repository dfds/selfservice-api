-- 2023-05-16 19:39:17 : add KafkaClusterAccess

create table "KafkaClusterAccess"
(
    "Id" uuid not null,
    "CapabilityId" varchar(255) not null,
    "KafkaClusterId" varchar(255) not null,
    "CreatedBy" varchar(255) not null,
    "CreatedAt" timestamp not null,
    "GrantedAt" timestamp null,

    constraint "KafkaClusterAccess_PK" primary key ("Id"),
    constraint "KafkaClusterAccess_Capability_Id_FK" foreign key ("CapabilityId") references "Capability" ("Id") on delete cascade,
    constraint "KafkaClusterAccess_KafkaCluster_Id_FK" foreign key ("KafkaClusterId") references "KafkaCluster" ("Id") on delete cascade,
    unique ("CapabilityId", "KafkaClusterId")
);

insert into "KafkaClusterAccess" ("Id", "CapabilityId", "KafkaClusterId", "CreatedBy", "CreatedAt", "GrantedAt")
select
    uuid_generate_v4(),
    "CapabilityId",
    "KafkaClusterId",
    'SYSTEM',
    now(),
    now()
from "KafkaTopic" kt
inner join "Capability" c on c."Id" = kt."CapabilityId"
inner join "KafkaCluster" kc on kc."Id" = kt."KafkaClusterId"
group by kt."CapabilityId", kt."KafkaClusterId";
