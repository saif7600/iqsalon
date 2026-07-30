START TRANSACTION;
ALTER TABLE "TillSessions" ADD "VarianceApprovalNote" text;

ALTER TABLE "TillSessions" ADD "VarianceApprovedAtUtc" timestamp with time zone;

ALTER TABLE "TillSessions" ADD "VarianceApprovedByUserId" uuid;

ALTER TABLE "DiscountApprovalRequests" ADD "AppliedAtUtc" timestamp with time zone;

ALTER TABLE "DiscountApprovalRequests" ADD "AppliedByUserId" uuid;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730055702_CommercialCompletionControls', '10.0.0');

COMMIT;

