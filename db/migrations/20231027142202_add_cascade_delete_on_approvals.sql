-- 2023-10-27 14:22:02 : add cascade delete on approvals

-- Entity Framework insists on setting this field null when membership applications are deleted, 
-- but so we will now let it temporarily be null before the delete cascade deletes it for good.
ALTER TABLE "MembershipApproval"
    ADD CONSTRAINT "MembershipApproval_MembershipApplicationId_FK" FOREIGN KEY ("MembershipApplicationId") REFERENCES "MembershipApplication" ("Id") ON DELETE CASCADE;
