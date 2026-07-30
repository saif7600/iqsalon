START TRANSACTION;
CREATE TABLE "CommissionLedgerEntries" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "StaffMemberId" uuid NOT NULL,
    "CommissionPlanId" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "SaleLineId" uuid,
    "RefundId" uuid,
    "EntryType" text NOT NULL,
    "Basis" text NOT NULL,
    "BasisAmount" numeric(18,2) NOT NULL,
    "RatePercentage" numeric(18,2) NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "IdempotencyKey" text NOT NULL,
    "BusinessDate" date NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_CommissionLedgerEntries" PRIMARY KEY ("Id")
);

CREATE TABLE "CommissionPlans" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "Basis" text NOT NULL,
    "ServiceRatePercentage" numeric(18,2) NOT NULL,
    "ProductRatePercentage" numeric(18,2) NOT NULL,
    "IncludeTips" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_CommissionPlans" PRIMARY KEY ("Id")
);

CREATE TABLE "StaffCommissionAssignments" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "StaffMemberId" uuid NOT NULL,
    "CommissionPlanId" uuid NOT NULL,
    "EffectiveFrom" date NOT NULL,
    "EffectiveTo" date,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_StaffCommissionAssignments" PRIMARY KEY ("Id")
);

CREATE INDEX "IX_CommissionLedgerEntries_TenantId_BranchId_StaffMemberId_Bus~" ON "CommissionLedgerEntries" ("TenantId", "BranchId", "StaffMemberId", "BusinessDate");

CREATE UNIQUE INDEX "IX_CommissionLedgerEntries_TenantId_IdempotencyKey" ON "CommissionLedgerEntries" ("TenantId", "IdempotencyKey");

CREATE UNIQUE INDEX "IX_CommissionPlans_TenantId_OrganizationId_Code" ON "CommissionPlans" ("TenantId", "OrganizationId", "Code");

CREATE INDEX "IX_StaffCommissionAssignments_TenantId_StaffMemberId_BranchId_~" ON "StaffCommissionAssignments" ("TenantId", "StaffMemberId", "BranchId", "EffectiveFrom");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730051616_CommissionLedger', '10.0.0');

COMMIT;

