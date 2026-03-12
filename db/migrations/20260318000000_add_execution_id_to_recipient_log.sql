ALTER TABLE "EmailCampaignRecipientLog"
    ADD COLUMN IF NOT EXISTS "ExecutionId" UUID NULL REFERENCES "EmailCampaignExecution"("Id") ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS "IX_EmailCampaignRecipientLog_ExecutionId"
    ON "EmailCampaignRecipientLog" ("ExecutionId")
    WHERE "ExecutionId" IS NOT NULL;
