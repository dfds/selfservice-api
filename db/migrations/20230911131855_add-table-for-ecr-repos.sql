-- 2023-09-11 13:18:55 : add-table-for-ecr-repos

CREATE TABLE "ECRRepository"
(
    "Id"             UUID          NOT NULL,
    "Name"           VARCHAR(255)  NOT NULL,
    "Description"    VARCHAR(1024) NOT NULL,
    "RepositoryName" VARCHAR(255)  NOT NULL,
    "CreatedBy"      VARCHAR(255)  NOT NULL,

    CONSTRAINT "ECRRepository_PK" PRIMARY KEY ("Id")
);
