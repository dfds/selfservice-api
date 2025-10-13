-- 2025-10-10 12:52:17 : add-user-settings-to-member
ALTER TABLE "Member" ADD COLUMN "UserSettings" JSONB DEFAULT '{}' NOT NULL;
