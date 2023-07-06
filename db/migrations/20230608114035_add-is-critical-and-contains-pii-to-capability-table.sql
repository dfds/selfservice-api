-- 2023-06-08 11:40:35 : add Is Critical and contains PII to capability

alter table "Capability"
add column "IsCritical" boolean,
add column "ContainsPII" boolean;
