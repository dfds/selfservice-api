-- Indexes on the capability-id (and user-id) filter columns used by the bulk email-campaign
-- rendering path (one WHERE col = ANY(...) query per data source) and by existing single-capability
-- lookups. PostgreSQL does not auto-index foreign-key referencing columns, so without these each
-- query is a sequential scan whose cost grows with table size regardless of the number of ids.

CREATE INDEX IF NOT EXISTS "IX_Membership_CapabilityId" ON "Membership" ("CapabilityId");
CREATE INDEX IF NOT EXISTS "IX_Membership_UserId" ON "Membership" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AwsAccount_CapabilityId" ON "AwsAccount" ("CapabilityId");
CREATE INDEX IF NOT EXISTS "IX_AzureResource_CapabilityId" ON "AzureResource" ("CapabilityId");
CREATE INDEX IF NOT EXISTS "IX_MembershipApplication_CapabilityId_Status" ON "MembershipApplication" ("CapabilityId", "Status");
