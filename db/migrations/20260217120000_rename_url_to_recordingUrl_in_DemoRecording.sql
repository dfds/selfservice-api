-- Migration to support several urls in DemoRecording table
ALTER TABLE "DemoRecording" RENAME COLUMN "Url" TO "RecordingUrl";
ALTER TABLE "DemoRecording" ADD COLUMN "SlidesUrl" TEXT;
ALTER TABLE "DemoRecording" ALTER COLUMN "RecordingUrl" TYPE TEXT;
