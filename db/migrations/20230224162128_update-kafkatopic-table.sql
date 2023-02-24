-- 2023-02-24 16:21:28 : update-kafkatopic-table

ALTER TABLE "KafkaTopic"
    DROP CONSTRAINT "KafkaTopic_Capability_Id_FK",
    DROP CONSTRAINT "KafkaTopic_KafkaCluster_Id_FK";