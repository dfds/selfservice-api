-- 2025-07-16 11:04:16 : drop-release-note-history-foreign-key

ALTER TABLE "ReleaseNoteHistory"
DROP CONSTRAINT IF EXISTS "ReleaseNoteHistory_ReleaseNoteId_fkey";
