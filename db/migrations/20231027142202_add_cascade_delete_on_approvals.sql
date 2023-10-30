-- 2023-10-27 14:22:02 : add cascade delete on approvals

ALTER TABLE "MembershipApproval"
    ADD CONSTRAINT "MembershipApproval_MembershipApplicationId_FK" FOREIGN KEY ("MembershipApplicationId") REFERENCES "MembershipApplication" ("Id") ON DELETE CASCADE;
