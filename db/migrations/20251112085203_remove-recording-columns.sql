-- 2025-11-12 08:52:03 : remove-recording-columns
ALTER TABLE "DemoRecording" DROP COLUMN "Tags";
ALTER TABLE "DemoRecording" DROP COLUMN "IsActive";
