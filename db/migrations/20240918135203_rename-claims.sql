-- 2024-09-18 13:52:03 : rename-claims

ALTER TABLE "CapabilityClaim" RENAME TO "SelfAssessment";
ALTER TABLE "SelfAssessment" RENAME COLUMN "Claim" TO "SelfAssessmentType";
ALTER INDEX "CapabilityClaim_PK" RENAME TO "SelfAssessment_PK";
