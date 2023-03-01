-- 2023-02-28 15:17:35 : add-missing-migrations-for-kafkatopic-table

-- update prod
update "KafkaTopic" set "KafkaClusterId" = 'lkc-4npj6'
where "KafkaClusterId" = '92e49432-d3d1-4e6c-b5ab-f7b7cb7c9a9b';

-- update dev
update "KafkaTopic" set "KafkaClusterId" = 'lkc-3wqzw'
where "KafkaClusterId" = '9cc6e73a-f62a-4f12-8fb5-3b489e39fb0a';
