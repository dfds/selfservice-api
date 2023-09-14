-- 2023-09-14 13:30:46 : add json schema table

CREATE TABLE "SelfServiceJsonSchema" (
    "Id" UUID NOT NULL,
    "ObjectId" VARCHAR(255) NOT NULL,
    "SchemaVersion" INT NOT NULL,
    "Schema" JSONB NOT NULL
);