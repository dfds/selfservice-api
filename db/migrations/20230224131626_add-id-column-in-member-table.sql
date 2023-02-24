-- 2023-02-24 13:16:26 : add-id-column-in-member-table

ALTER TABLE "Member" 
     ADD COLUMN "Id" varchar(255);

UPDATE "Member" SET "Id" = "UPN";

ALTER TABLE "Membership" 
	DROP CONSTRAINT "Membership_Member_UPN_FK";

ALTER TABLE "Member" 
	DROP CONSTRAINT "Member_PK";

ALTER TABLE "Member" 
	ADD CONSTRAINT "Member_PK" PRIMARY KEY ("Id");
    
ALTER TABLE "Member" 
	DROP COLUMN "UPN";
