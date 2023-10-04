-- 2023-10-04 09:30:00 : remove ecr repositoryname
ALTER TABLE "ECRRepository" DROP COLUMN "RepositoryName";
ALTER TABLE "ECRRepository" ADD UNIQUE ("Name");
