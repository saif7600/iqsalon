START TRANSACTION;
CREATE TABLE "CreditNotes" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "InvoiceId" uuid NOT NULL,
    "CreditNoteNumber" text NOT NULL,
    "CurrencyCode" text NOT NULL,
    "Subtotal" numeric(18,2) NOT NULL,
    "TaxTotal" numeric(18,2) NOT NULL,
    "GrandTotal" numeric(18,2) NOT NULL,
    "Reason" text NOT NULL,
    "Status" text NOT NULL,
    "IssuedByUserId" uuid NOT NULL,
    "IssuedAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_CreditNotes" PRIMARY KEY ("Id")
);

CREATE TABLE "CustomerDeposits" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "CustomerId" uuid NOT NULL,
    "PaymentId" uuid NOT NULL,
    "DepositNumber" text NOT NULL,
    "CurrencyCode" text NOT NULL,
    "OriginalAmount" numeric(18,2) NOT NULL,
    "AvailableAmount" numeric(18,2) NOT NULL,
    "Status" text NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_CustomerDeposits" PRIMARY KEY ("Id")
);

CREATE TABLE "DepositApplications" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "DepositId" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "AppliedByUserId" uuid NOT NULL,
    "AppliedAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_DepositApplications" PRIMARY KEY ("Id")
);

CREATE TABLE "DiscountApprovalRequests" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "RequestedAmount" numeric(18,2) NOT NULL,
    "RequestedPercentage" numeric(18,2) NOT NULL,
    "Reason" text NOT NULL,
    "Status" text NOT NULL,
    "RequestedByUserId" uuid NOT NULL,
    "DecidedByUserId" uuid,
    "DecisionNote" text,
    "DecidedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_DiscountApprovalRequests" PRIMARY KEY ("Id")
);

CREATE TABLE "Refunds" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "CreditNoteId" uuid NOT NULL,
    "PaymentId" uuid NOT NULL,
    "Amount" numeric(18,2) NOT NULL,
    "Reason" text NOT NULL,
    "IdempotencyKey" text NOT NULL,
    "RefundedByUserId" uuid NOT NULL,
    "RefundedAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_Refunds" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_CreditNotes_TenantId_OrganizationId_CreditNoteNumber" ON "CreditNotes" ("TenantId", "OrganizationId", "CreditNoteNumber");

CREATE UNIQUE INDEX "IX_CustomerDeposits_TenantId_OrganizationId_DepositNumber" ON "CustomerDeposits" ("TenantId", "OrganizationId", "DepositNumber");

CREATE INDEX "IX_DepositApplications_TenantId_DepositId_SaleId" ON "DepositApplications" ("TenantId", "DepositId", "SaleId");

CREATE INDEX "IX_DiscountApprovalRequests_TenantId_SaleId_Status" ON "DiscountApprovalRequests" ("TenantId", "SaleId", "Status");

CREATE UNIQUE INDEX "IX_Refunds_TenantId_IdempotencyKey" ON "Refunds" ("TenantId", "IdempotencyKey");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730044948_CommercialControls', '10.0.0');

COMMIT;

