START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730091942_MobileExperienceSecurity') THEN
    CREATE TABLE "MobileSessions" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "ActorType" text NOT NULL,
        "UserId" uuid,
        "StaffMemberId" uuid,
        "CustomerId" uuid,
        "Channel" text NOT NULL,
        "TokenHash" text NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "LastSeenAtUtc" timestamp with time zone NOT NULL,
        "RevokedAtUtc" timestamp with time zone,
        "RevocationReason" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MobileSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730091942_MobileExperienceSecurity') THEN
    CREATE TABLE "MobileVerificationChallenges" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "CustomerId" uuid NOT NULL,
        "Channel" text NOT NULL,
        "DestinationHash" text NOT NULL,
        "CodeHash" text NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "ConsumedAtUtc" timestamp with time zone,
        "FailedAttemptCount" integer NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_MobileVerificationChallenges" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730091942_MobileExperienceSecurity') THEN
    CREATE INDEX "IX_MobileSessions_TenantId_ActorType_ExpiresAtUtc" ON "MobileSessions" ("TenantId", "ActorType", "ExpiresAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730091942_MobileExperienceSecurity') THEN
    CREATE UNIQUE INDEX "IX_MobileSessions_TokenHash" ON "MobileSessions" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730091942_MobileExperienceSecurity') THEN
    CREATE INDEX "IX_MobileVerificationChallenges_TenantId_CustomerId_CreatedAtU~" ON "MobileVerificationChallenges" ("TenantId", "CustomerId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730091942_MobileExperienceSecurity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260730091942_MobileExperienceSecurity', '10.0.0');
    END IF;
END $EF$;
COMMIT;

