-- 2023-05-28 14:36:25 : add Kafka endpoints to cluster info

alter table "KafkaCluster"
add column "BootstrapServers" varchar(255) not null default('localhost:9092'),
add column "SchemaRegistryUrl" varchar(255) not null default('localhost');
