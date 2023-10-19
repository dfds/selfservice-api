-- 2023-10-19 08:24:13 : invitations

CREATE TABLE "Invitation"
(
    "Id"          UUID          NOT NULL,
    "Invitee"     UUID          NOT NULL,
    "Target"      UUID          NOT NULL,
    "Status"      VARCHAR(255)  NOT NULL,
    "CreatedBy"   VARCHAR(255)  NOT NULL,
    "CreatedAt"   TIMESTAMP     NOT NULL,

    CONSTRAINT "Invitation_PK" PRIMARY KEY ("Id")
);
