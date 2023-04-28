-- 2023-04-28 10:35:24 : change-payload-of-pgnotify-for-outbox
CREATE OR REPLACE FUNCTION public._outbox_notifier()
RETURNS trigger
LANGUAGE plpgsql
AS $function$
BEGIN
    PERFORM pg_notify('dafda_outbox','');
    RETURN NEW;
END;
$function$
;
