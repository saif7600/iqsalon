START TRANSACTION;
ALTER TABLE "OrganizationCommercialSettings" ADD "NextMembershipSequence" bigint NOT NULL DEFAULT 1;

CREATE TABLE "CustomerMemberships" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "MembershipPlanId" uuid NOT NULL,
    "EnrollmentSaleId" uuid NOT NULL,
    "MembershipNumber" text NOT NULL,
    "Status" text NOT NULL,
    "StartsAtUtc" timestamp with time zone NOT NULL,
    "EndsAtUtc" timestamp with time zone,
    "NextBillingAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_CustomerMemberships" PRIMARY KEY ("Id")
);

CREATE TABLE "MembershipLedgerEntries" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "CustomerMembershipId" uuid NOT NULL,
    "SaleId" uuid,
    "AppointmentId" uuid,
    "EntryType" text NOT NULL,
    "Credits" numeric(18,2) NOT NULL,
    "IdempotencyKey" text NOT NULL,
    "Reference" text,
    "CreatedByUserId" uuid NOT NULL,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_MembershipLedgerEntries" PRIMARY KEY ("Id")
);

CREATE TABLE "MembershipPlans" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text,
    "RecurringPrice" numeric(18,2) NOT NULL,
    "CurrencyCode" text NOT NULL,
    "BillingInterval" text NOT NULL,
    "IncludedCredits" numeric(18,2) NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_MembershipPlans" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_CustomerMemberships_TenantId_EnrollmentSaleId" ON "CustomerMemberships" ("TenantId", "EnrollmentSaleId");

CREATE UNIQUE INDEX "IX_CustomerMemberships_TenantId_OrganizationId_MembershipNumber" ON "CustomerMemberships" ("TenantId", "OrganizationId", "MembershipNumber");

CREATE UNIQUE INDEX "IX_MembershipLedgerEntries_TenantId_IdempotencyKey" ON "MembershipLedgerEntries" ("TenantId", "IdempotencyKey");

CREATE UNIQUE INDEX "IX_MembershipPlans_TenantId_OrganizationId_Code" ON "MembershipPlans" ("TenantId", "OrganizationId", "Code");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730050215_MembershipLedger', '10.0.0');

COMMIT;

