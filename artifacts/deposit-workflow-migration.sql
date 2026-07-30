START TRANSACTION;
ALTER TABLE "OrganizationCommercialSettings" ADD "NextDepositSequence" bigint NOT NULL DEFAULT 1;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730045353_DepositWorkflow', '10.0.0');

COMMIT;

