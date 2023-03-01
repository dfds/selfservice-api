-- 2023-03-01 13:14:50 : change-type-of-retention

alter table "KafkaTopic"
	add column "Retention_new" varchar(20);

update "KafkaTopic" set "Retention_new" = (
	case 
		when "Retention" = -1 then 'forever' 
		else concat(("Retention" / 1000 / 60 / 60 / 24), 'd')
	end
);

alter table "KafkaTopic"
	drop column "Retention";

alter table "KafkaTopic"
	rename column "Retention_new" to "Retention";

ALTER TABLE "KafkaTopic"
    ALTER COLUMN "Retention" SET NOT NULL;
