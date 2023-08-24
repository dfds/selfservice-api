-- 2023-08-16 10:30:xx : Add ModifiedBy to capabilities

ALTER TABLE "Capability"
ADD COLUMN "ModifiedBy"  varchar(255) default null;
