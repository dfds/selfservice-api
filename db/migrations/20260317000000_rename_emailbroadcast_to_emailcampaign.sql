-- Rename tables
ALTER TABLE "EmailBroadcast" RENAME TO "EmailCampaign";
ALTER TABLE "EmailBroadcastRecipientLog" RENAME TO "EmailCampaignRecipientLog";
ALTER TABLE "EmailBroadcastExecution" RENAME TO "EmailCampaignExecution";

-- Rename FK columns in child tables
ALTER TABLE "EmailCampaignRecipientLog" RENAME COLUMN "EmailBroadcastId" TO "EmailCampaignId";
ALTER TABLE "EmailCampaignExecution" RENAME COLUMN "EmailBroadcastId" TO "EmailCampaignId";

-- Rename constraints
ALTER TABLE "EmailCampaign" RENAME CONSTRAINT "EmailBroadcast_PK" TO "EmailCampaign_PK";
ALTER TABLE "EmailCampaignRecipientLog" RENAME CONSTRAINT "EmailBroadcastRecipientLog_PK" TO "EmailCampaignRecipientLog_PK";
ALTER TABLE "EmailCampaignRecipientLog" RENAME CONSTRAINT "EmailBroadcastRecipientLog_Broadcast_FK" TO "EmailCampaignRecipientLog_Campaign_FK";

-- Rename indexes
ALTER INDEX "IX_EmailBroadcastRecipientLog_BroadcastId" RENAME TO "IX_EmailCampaignRecipientLog_CampaignId";
ALTER INDEX "IX_EmailBroadcastExecution_BroadcastId" RENAME TO "IX_EmailCampaignExecution_CampaignId";
ALTER INDEX "IX_EmailBroadcast_IsDeleted" RENAME TO "IX_EmailCampaign_IsDeleted";
