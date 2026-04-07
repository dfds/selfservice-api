-- Create new events table to replace demos
-- Part 1 of 3: Creating new table structure

-- Create the new events table
CREATE TABLE "Event" (
    "Id" UUID NOT NULL,
    "EventDate" TIMESTAMP NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "Type" TEXT NOT NULL,  -- 'Demo', 'Workshop', 'Informational', 'Other'
    "CreatedBy" TEXT NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    CONSTRAINT "Event_PK" PRIMARY KEY ("Id")
);

-- Create the event_attachments table
CREATE TABLE "EventAttachment" (
    "Id" UUID NOT NULL,
    "EventId" UUID NOT NULL,
    "Url" TEXT NOT NULL,
    "AttachmentType" TEXT NOT NULL,  -- 'Document', 'Recording', 'Image', 'Other'
    "Description" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL,
    CONSTRAINT "EventAttachment_PK" PRIMARY KEY ("Id"),
    CONSTRAINT "EventAttachment_Event_FK" FOREIGN KEY ("EventId") REFERENCES "Event"("Id") ON DELETE CASCADE
);

-- Create indexes for performance
CREATE INDEX "IDX_Event_EventDate" ON "Event"("EventDate");
CREATE INDEX "IDX_Event_Type" ON "Event"("Type");
CREATE INDEX "IDX_EventAttachment_EventId" ON "EventAttachment"("EventId");
CREATE INDEX "IDX_EventAttachment_Type" ON "EventAttachment"("AttachmentType");
