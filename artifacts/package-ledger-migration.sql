START TRANSACTION;
ALTER TABLE "OrganizationCommercialSettings" ADD "NextPackageSequence" bigint NOT NULL DEFAULT 1;

CREATE TABLE "CustomerPackages" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "PackageDefinitionId" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "PackageNumber" text NOT NULL,
    "Status" text NOT NULL,
    "PurchasedAtUtc" timestamp with time zone NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_CustomerPackages" PRIMARY KEY ("Id")
);

CREATE TABLE "PackageDefinitions" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "Price" numeric(18,2) NOT NULL,
    "CurrencyCode" text NOT NULL,
    "ValidityDays" integer NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_PackageDefinitions" PRIMARY KEY ("Id")
);

CREATE TABLE "PackageEntitlements" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "PackageDefinitionId" uuid NOT NULL,
    "ServiceId" uuid NOT NULL,
    "Quantity" numeric(18,2) NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_PackageEntitlements" PRIMARY KEY ("Id")
);

CREATE TABLE "PackageLedgerEntries" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "CustomerPackageId" uuid NOT NULL,
    "ServiceId" uuid NOT NULL,
    "SaleId" uuid,
    "AppointmentId" uuid,
    "EntryType" text NOT NULL,
    "Quantity" numeric(18,2) NOT NULL,
    "IdempotencyKey" text NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_PackageLedgerEntries" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_CustomerPackages_TenantId_OrganizationId_PackageNumber" ON "CustomerPackages" ("TenantId", "OrganizationId", "PackageNumber");

CREATE UNIQUE INDEX "IX_CustomerPackages_TenantId_SaleId" ON "CustomerPackages" ("TenantId", "SaleId");

CREATE UNIQUE INDEX "IX_PackageDefinitions_TenantId_OrganizationId_Code" ON "PackageDefinitions" ("TenantId", "OrganizationId", "Code");

CREATE UNIQUE INDEX "IX_PackageEntitlements_TenantId_PackageDefinitionId_ServiceId" ON "PackageEntitlements" ("TenantId", "PackageDefinitionId", "ServiceId");

CREATE UNIQUE INDEX "IX_PackageLedgerEntries_TenantId_IdempotencyKey" ON "PackageLedgerEntries" ("TenantId", "IdempotencyKey");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730045848_PackageLedger', '10.0.0');

COMMIT;

