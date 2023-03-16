-- 2023-02-28 13:23:52 : change-kafkacluster-id

alter table "KafkaTopic" 
    ALTER COLUMN "KafkaClusterId" TYPE varchar(255);

update "KafkaTopic" as kt 
set "KafkaClusterId" = c."RealClusterId"
from "KafkaCluster" c 
where c."Id" = kt."Id";
	

alter table "KafkaCluster"
	alter column "Id" type varchar(255);

update "KafkaCluster" set "Id" = "RealClusterId";

alter table "KafkaCluster"
    drop column "RealClusterId";