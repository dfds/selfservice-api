-- Migrate data from DemoRecording to events
-- Part 2 of 3: Data migration

-- Migrate data from demos to events
INSERT INTO "Event" ("Id", "EventDate", "Title", "Description", "Type", "CreatedBy", "CreatedAt")
SELECT 
    "Id",
    "RecordingDate",
    "Title",
    "Description",
    'Demo',  -- All old demos become type 'Demo'
    "CreatedBy",
    "CreatedAt"
FROM "DemoRecording";

-- Migrate recording URLs to event_attachments
INSERT INTO "EventAttachment" ("Id", "EventId", "Url", "AttachmentType", "Description", "CreatedAt")
SELECT 
    gen_random_uuid(),
    "Id",
    "RecordingUrl",
    'Recording',
    'Migrated recording',
    "CreatedAt"
FROM "DemoRecording"
WHERE "RecordingUrl" IS NOT NULL AND "RecordingUrl" != '';

-- Migrate slides URLs to event_attachments
INSERT INTO "EventAttachment" ("Id", "EventId", "Url", "AttachmentType", "Description", "CreatedAt")
SELECT 
    gen_random_uuid(),
    "Id",
    "SlidesUrl",
    'Document',
    'Migrated slides',
    "CreatedAt"
FROM "DemoRecording"
WHERE "SlidesUrl" IS NOT NULL AND "SlidesUrl" != '';
