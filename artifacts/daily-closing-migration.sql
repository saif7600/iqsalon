START TRANSACTION;
CREATE TABLE "BranchDailyClosings" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "BusinessDate" date NOT NULL,
    "Status" text NOT NULL,
    "CurrencyCode" text NOT NULL,
    "GrossSales" numeric(18,2) NOT NULL,
    "Discounts" numeric(18,2) NOT NULL,
    "NetSales" numeric(18,2) NOT NULL,
    "TaxTotal" numeric(18,2) NOT NULL,
    "Tips" numeric(18,2) NOT NULL,
    "PaymentsIn" numeric(18,2) NOT NULL,
    "RefundsOut" numeric(18,2) NOT NULL,
    "ExpectedCash" numeric(18,2) NOT NULL,
    "CountedCash" numeric(18,2) NOT NULL,
    "CashVariance" numeric(18,2) NOT NULL,
    "PostedSaleCount" integer NOT NULL,
    "InvoiceCount" integer NOT NULL,
    "RefundCount" integer NOT NULL,
    "VatSummaryJson" text NOT NULL,
    "PaymentSummaryJson" text NOT NULL,
    "TillSummaryJson" text NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "ApprovedByUserId" uuid,
    "ApprovalNote" text,
    "ClosedAtUtc" timestamp with time zone NOT NULL,
    "ApprovedAtUtc" timestamp with time zone,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_BranchDailyClosings" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_BranchDailyClosings_TenantId_BranchId_BusinessDate" ON "BranchDailyClosings" ("TenantId", "BranchId", "BusinessDate");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730052813_DailyClosing', '10.0.0');

COMMIT;

