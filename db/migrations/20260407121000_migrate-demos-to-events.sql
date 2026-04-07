-- Migrate data from DemoRecording to events
-- Part 2 of 3: Data migration

-- Migrate data from demos to events
INSERT INTO events (id, event_date, title, description, type, created_by, created_at)
SELECT 
    id,
    recording_date as event_date,
    title,
    description,
    'Demo' as type,  -- All old demos become type 'Demo'
    created_by,
    created_at
FROM "DemoRecording";

-- Migrate recording URLs to event_attachments
INSERT INTO event_attachments (id, event_id, url, attachment_type, description, created_at)
SELECT 
    gen_random_uuid() as id,
    id as event_id,
    recording_url as url,
    'Recording' as attachment_type,
    'Migrated recording' as description,
    created_at
FROM "DemoRecording"
WHERE recording_url IS NOT NULL AND recording_url != '';

-- Migrate slides URLs to event_attachments
INSERT INTO event_attachments (id, event_id, url, attachment_type, description, created_at)
SELECT 
    gen_random_uuid() as id,
    id as event_id,
    slides_url as url,
    'Document' as attachment_type,
    'Migrated slides' as description,
    created_at
FROM "DemoRecording"
WHERE slides_url IS NOT NULL AND slides_url != '';
