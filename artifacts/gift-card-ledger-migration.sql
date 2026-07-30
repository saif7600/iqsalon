START TRANSACTION;
ALTER TABLE "OrganizationCommercialSettings" ADD "NextGiftCardSequence" bigint NOT NULL DEFAULT 1;

CREATE TABLE "GiftCardLedgerEntries" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "GiftCardId" uuid NOT NULL,
    "SaleId" uuid,
    "PaymentId" uuid,
    "EntryType" text NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "IdempotencyKey" text NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_GiftCardLedgerEntries" PRIMARY KEY ("Id")
);

CREATE TABLE "GiftCards" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "IssuanceSaleId" uuid NOT NULL,
    "GiftCardNumber" text NOT NULL,
    "CodeHash" text NOT NULL,
    "CodeLastFour" text NOT NULL,
    "CurrencyCode" text NOT NULL,
    "InitialValue" numeric(18,2) NOT NULL,
    "Status" text NOT NULL,
    "CustomerId" uuid,
    "IssuedAtUtc" timestamp with time zone NOT NULL,
    "ExpiresAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_GiftCards" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_GiftCardLedgerEntries_TenantId_IdempotencyKey" ON "GiftCardLedgerEntries" ("TenantId", "IdempotencyKey");

CREATE UNIQUE INDEX "IX_GiftCards_TenantId_CodeHash" ON "GiftCards" ("TenantId", "CodeHash");

CREATE UNIQUE INDEX "IX_GiftCards_TenantId_IssuanceSaleId" ON "GiftCards" ("TenantId", "IssuanceSaleId");

CREATE UNIQUE INDEX "IX_GiftCards_TenantId_OrganizationId_GiftCardNumber" ON "GiftCards" ("TenantId", "OrganizationId", "GiftCardNumber");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730050506_GiftCardLedger', '10.0.0');

COMMIT;

