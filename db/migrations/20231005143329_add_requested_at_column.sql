-- 2023-10-05 14:33:29 : add requested at column

ALTER TABLE "ECRRepository"
    ADD COLUMN "RequestedAt" TIMESTAMP DEFAULT NULL;