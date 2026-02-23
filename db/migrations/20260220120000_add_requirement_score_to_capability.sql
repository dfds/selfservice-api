-- Add requirement_score column to Capability table
ALTER TABLE "Capability" 
ADD COLUMN "RequirementScore" DOUBLE PRECISION NULL;
