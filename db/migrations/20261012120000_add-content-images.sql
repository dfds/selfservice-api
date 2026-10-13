CREATE TABLE "ContentImage" (
    "Id" UUID NOT NULL,
    "OwnerType" TEXT NOT NULL,
    "OwnerId" UUID NOT NULL,
    "ObjectKey" TEXT NOT NULL,
    "FileName" TEXT NOT NULL,
    "ContentType" TEXT NOT NULL,
    "Length" BIGINT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "IsDeletePending" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "ContentImage_PK" PRIMARY KEY ("Id"),
    CONSTRAINT "ContentImage_OwnerType_Check" CHECK ("OwnerType" IN ('NewsItem', 'Event', 'ReleaseNote', 'EmailCampaign')),
    CONSTRAINT "ContentImage_Length_Check" CHECK ("Length" > 0)
);

CREATE INDEX "IX_ContentImage_Owner" ON "ContentImage" ("OwnerType", "OwnerId");
CREATE UNIQUE INDEX "UX_ContentImage_ObjectKey" ON "ContentImage" ("ObjectKey");