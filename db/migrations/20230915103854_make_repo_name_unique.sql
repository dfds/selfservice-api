-- 2023-09-15 10:38:54 : make repo name unique

ALTER TABLE "ECRRepository"
    ADD UNIQUE ("RepositoryName");