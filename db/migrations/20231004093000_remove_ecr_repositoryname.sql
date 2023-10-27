-- 2023-10-04 09:30:00 : remove ecr repositoryname
UPDATE "ECRRepository" SET "Name" = "RepositoryName";
ALTER TABLE "ECRRepository" DROP COLUMN "RepositoryName";
ALTER TABLE "ECRRepository" ADD UNIQUE ("Name");
