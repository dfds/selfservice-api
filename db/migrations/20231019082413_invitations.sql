-- 2023-10-19 08:24:13 : invitations

CREATE TABLE "Invitation"
(
    "Id"          UUID          NOT NULL,
    "Invitee"     VARCHAR(255)  NOT NULL, -- email of the invitee which maps to `Member`-id, which is also just email
    "Description" VARCHAR(255)  NOT NULL,
    "TargetId"    VARCHAR(255)  NOT NULL,
    "TargetType"  VARCHAR(255)  NOT NULL,
    "Status"      VARCHAR(255)  NOT NULL,
    "CreatedBy"   VARCHAR(255)  NOT NULL,
    "CreatedAt"   TIMESTAMP     NOT NULL,
    "ModifiedAt"  TIMESTAMP     NOT NULL,

    CONSTRAINT "Invitation_PK" PRIMARY KEY ("Id")
);
