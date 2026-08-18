CREATE TABLE "KubernetesAccess" (
    "Id" uuid PRIMARY KEY,
    "CapabilityId" varchar(255) NOT NULL,
    "Environment" varchar(255) NOT NULL,
    "AwsAccountId" uuid NULL,
    "RequestedAt" timestamp NOT NULL,
    "RequestedBy" varchar(255) NOT NULL,
    "Namespace" varchar(255) NULL,
    "GrantedAt" timestamp NULL
);
