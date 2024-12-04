-- 2024-11-25 13:34:04 : add-selfassessment-status

ALTER TABLE "SelfAssessment" ADD COLUMN "Status" varchar(32);
