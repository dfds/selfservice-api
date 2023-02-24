-- 2023-02-24 13:46:10 : update-membership-table

ALTER TABLE "Membership"
    DROP constraint "Membership_PK",
    DROP constraint "Membership_Capability_Id_FK";

ALTER TABLE "Membership"
    RENAME COLUMN "UPN" TO "UserId";

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

ALTER TABLE "Membership" 
     ADD COLUMN "Id" uuid DEFAULT uuid_generate_v4();

ALTER TABLE "Membership" 
	ADD CONSTRAINT "Membership_PK" PRIMARY KEY ("Id");