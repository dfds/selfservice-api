CREATE TABLE IF NOT EXISTS "EmailBroadcastExecution" (
    "Id"                  UUID PRIMARY KEY,
    "EmailBroadcastId"    UUID NOT NULL REFERENCES "EmailBroadcast"("Id") ON DELETE CASCADE,
    "ExecutedAt"          TIMESTAMPTZ NOT NULL,
    "TotalRecipients"     INT NOT NULL,
    "SuccessCount"        INT NOT NULL DEFAULT 0,
    "FailureCount"        INT NOT NULL DEFAULT 0,
    "Status"              TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_EmailBroadcastExecution_BroadcastId"
    ON "EmailBroadcastExecution" ("EmailBroadcastId");
