-- 2023-02-24 15:54:46 : rename-colume-in-kafkacluster-table

ALTER TABLE "KafkaCluster"
    RENAME COLUMN "ClusterId" TO "RealClusterId";
