-- Add optional frontpage summary to news items.
-- When populated, this text is shown on the frontpage instead of the full body when the item is highlighted.

ALTER TABLE "NewsItem"
    ADD COLUMN "FrontpageSummary" TEXT;
