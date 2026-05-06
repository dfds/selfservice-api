-- Create the news item table

CREATE TABLE "NewsItem" (
    "Id" UUID NOT NULL,
    "Title" TEXT NOT NULL,
    "Body" TEXT NOT NULL,
    "DueDate" TIMESTAMP NOT NULL,
    "IsHighlighted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedBy" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "ModifiedAt" TIMESTAMP,
    CONSTRAINT "NewsItem_PK" PRIMARY KEY ("Id")
);

-- Only one item should be highlighted at a time
CREATE UNIQUE INDEX "UX_NewsItem_Highlighted" ON "NewsItem"("IsHighlighted") WHERE "IsHighlighted" = TRUE;

-- Index for the common query: fetch relevant (non-expired) news
CREATE INDEX "IDX_NewsItem_DueDate" ON "NewsItem"("DueDate");
