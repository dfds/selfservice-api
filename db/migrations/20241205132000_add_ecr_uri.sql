-- 2024-12-05 13:20:00 : add-ecr-uri

ALTER TABLE "ECRRepository" 
    ADD COLUMN "Uri" varchar(256) DEFAULT NULL;
