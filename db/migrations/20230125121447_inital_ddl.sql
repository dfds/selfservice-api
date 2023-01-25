-- 2023-01-25 12:14:47 : inital ddl

CREATE TABLE "ServiceCatalog" (
    "Id" uuid NOT NULL,
    "Name" varchar(255) NOT NULL,
    "Namespace" varchar(255) NOT NULL,
    "Spec" text NOT NULL,
    "CreatedAt" timestamp NOT NULL,

    CONSTRAINT "ServiceCatalog_PK" PRIMARY KEY("Id")
);
