-- 2023-10-13 13:01:53 : create team table

CREATE TABLE "Team"
(
    "Id"          UUID          NOT NULL,
    "Name"        VARCHAR(255)  NOT NULL,
    "Description" VARCHAR(1024) NOT NULL,
    "CreatedBy"   VARCHAR(255)  NOT NULL,
    "CreatedAt"   TIMESTAMP     NOT NULL,

    CONSTRAINT "Team_PK" PRIMARY KEY ("Id")
);
