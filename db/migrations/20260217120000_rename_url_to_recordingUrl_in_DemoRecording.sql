-- Migration to rename 'url' column to 'recordingUrl' in DemoRecording table
ALTER TABLE "DemoRecording" RENAME COLUMN "Url" TO "RecordingUrl";
ALTER TABLE "DemoRecording" ADD COLUMN "SlidesUrl" varchar(511);
