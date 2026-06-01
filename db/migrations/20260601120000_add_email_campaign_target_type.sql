-- Add TargetType to EmailCampaign. Existing rows are Capability-targeted.
alter table "EmailCampaign"
    add column "TargetType" text not null default 'Capability';

-- User-targeted campaigns address each user once, so a recipient log row no longer
-- needs to be scoped to a single capability. Make these nullable for the new path;
-- existing rows remain unchanged.
alter table "EmailCampaignRecipientLog"
    alter column "CapabilityId" drop not null,
    alter column "CapabilityName" drop not null;
