-- Create new events table to replace demos
-- Part 1 of 3: Creating new table structure

-- Create the new events table
CREATE TABLE events (
    id UUID PRIMARY KEY,
    event_date TIMESTAMP NOT NULL,
    title TEXT NOT NULL,
    description TEXT NOT NULL,
    type TEXT NOT NULL,  -- 'Demo', 'Workshop', 'Informational', 'Other'
    created_by TEXT NOT NULL,
    created_at TIMESTAMP NOT NULL
);

-- Create the event_attachments table
CREATE TABLE event_attachments (
    id UUID PRIMARY KEY,
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    url TEXT NOT NULL,
    attachment_type TEXT NOT NULL,  -- 'Document', 'Recording', 'Image', 'Other'
    description TEXT,
    created_at TIMESTAMP NOT NULL
);

-- Create indexes for performance
CREATE INDEX idx_events_event_date ON events(event_date);
CREATE INDEX idx_events_type ON events(type);
CREATE INDEX idx_event_attachments_event_id ON event_attachments(event_id);
CREATE INDEX idx_event_attachments_type ON event_attachments(attachment_type);
