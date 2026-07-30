START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "StaffServiceCapabilities" ALTER COLUMN "PriceOverride" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "ServiceBookingRules" ALTER COLUMN "DepositValue" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "ServiceAddOns" ALTER COLUMN "AdditionalPrice" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "SalonServices" ALTER COLUMN "DepositValue" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "SalonServices" ALTER COLUMN "BasePrice" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "BranchServices" ALTER COLUMN "PriceOverride" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "AppointmentServices" ALTER COLUMN "UnitPrice" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "AppointmentServices" ALTER COLUMN "TotalAmount" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "AppointmentServices" ALTER COLUMN "TaxAmount" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "AppointmentServices" ALTER COLUMN "DiscountAmount" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    ALTER TABLE "AppointmentServices" ALTER COLUMN "DepositValue" TYPE numeric(18,2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "BranchProducts" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "RetailPriceOverride" numeric(18,2),
        "TaxCodeOverrideId" uuid,
        "IsAvailableForSale" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_BranchProducts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "CashMovements" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "TillSessionId" uuid NOT NULL,
        "Type" text NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Reason" text NOT NULL,
        "CreatedByUserId" uuid NOT NULL,
        "OccurredAtUtc" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_CashMovements" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "Invoices" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "SaleId" uuid NOT NULL,
        "InvoiceNumber" text NOT NULL,
        "CurrencyCode" text NOT NULL,
        "Subtotal" numeric(18,2) NOT NULL,
        "DiscountTotal" numeric(18,2) NOT NULL,
        "TaxTotal" numeric(18,2) NOT NULL,
        "GrandTotal" numeric(18,2) NOT NULL,
        "TaxSummaryJson" text NOT NULL,
        "Status" text NOT NULL,
        "IssuedAtUtc" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Invoices" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "OrganizationCommercialSettings" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "DefaultCurrencyCode" text NOT NULL,
        "PricesIncludeTax" boolean NOT NULL,
        "InvoicePrefix" text NOT NULL,
        "CreditNotePrefix" text NOT NULL,
        "ReceiptPrefix" text NOT NULL,
        "NextSaleSequence" bigint NOT NULL,
        "NextInvoiceSequence" bigint NOT NULL,
        "NextCreditNoteSequence" bigint NOT NULL,
        "NextReceiptSequence" bigint NOT NULL,
        "NextPaymentSequence" bigint NOT NULL,
        "AllowSplitPayments" boolean NOT NULL,
        "AllowPartialPayments" boolean NOT NULL,
        "AllowOverpayment" boolean NOT NULL,
        "RequireTillSessionForCashPayments" boolean NOT NULL,
        "AllowTips" boolean NOT NULL,
        "RequireManagerForDiscountAbovePercentage" numeric(18,2) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_OrganizationCommercialSettings" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "PaymentAllocations" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "PaymentId" uuid NOT NULL,
        "SaleId" uuid,
        "InvoiceId" uuid,
        "DepositId" uuid,
        "Amount" numeric(18,2) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PaymentAllocations" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "PaymentMethods" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "Type" text NOT NULL,
        "RequiresReference" boolean NOT NULL,
        "RequiresTillSession" boolean NOT NULL,
        "SupportsRefund" boolean NOT NULL,
        "SupportsChange" boolean NOT NULL,
        "SupportsPartialPayment" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_PaymentMethods" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "Payments" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "PaymentNumber" text NOT NULL,
        "CustomerId" uuid,
        "PaymentMethodId" uuid NOT NULL,
        "Direction" text NOT NULL,
        "Status" text NOT NULL,
        "CurrencyCode" text NOT NULL,
        "Amount" numeric(18,2) NOT NULL,
        "Reference" text,
        "Provider" text NOT NULL,
        "ProviderTransactionId" text,
        "IdempotencyKey" text NOT NULL,
        "ReceivedByUserId" uuid,
        "TillSessionId" uuid,
        "OccurredAtUtc" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Payments" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "ProductCategories" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "Name" text NOT NULL,
        "LocalizedNameJson" text,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_ProductCategories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "Products" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "Name" text NOT NULL,
        "LocalizedNameJson" text,
        "Description" text,
        "Sku" text NOT NULL,
        "Barcode" text,
        "Brand" text,
        "UnitOfMeasure" text NOT NULL,
        "RetailPrice" numeric(18,2) NOT NULL,
        "CostPrice" numeric(18,2) NOT NULL,
        "CurrencyCode" text NOT NULL,
        "TaxCodeId" uuid,
        "TrackInventory" boolean NOT NULL,
        "AllowNegativeStock" boolean NOT NULL,
        "CommissionEligible" boolean NOT NULL,
        "IsRetail" boolean NOT NULL,
        "IsProfessionalUse" boolean NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Products" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "SaleLines" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "SaleId" uuid NOT NULL,
        "LineType" text NOT NULL,
        "ServiceId" uuid,
        "ProductId" uuid,
        "AppointmentServiceId" uuid,
        "DescriptionSnapshot" text NOT NULL,
        "SkuSnapshot" text,
        "Quantity" numeric(18,2) NOT NULL,
        "UnitPrice" numeric(18,2) NOT NULL,
        "GrossAmount" numeric(18,2) NOT NULL,
        "DiscountAmount" numeric(18,2) NOT NULL,
        "NetAmount" numeric(18,2) NOT NULL,
        "TaxCodeSnapshot" text NOT NULL,
        "TaxRateSnapshot" numeric(18,2) NOT NULL,
        "TaxInclusiveSnapshot" boolean NOT NULL,
        "TaxableAmount" numeric(18,2) NOT NULL,
        "TaxAmount" numeric(18,2) NOT NULL,
        "LineTotal" numeric(18,2) NOT NULL,
        "AssignedStaffMemberId" uuid,
        "CommissionEligible" boolean NOT NULL,
        "CostSnapshot" numeric(18,2) NOT NULL,
        "Sequence" integer NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_SaleLines" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "Sales" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "SaleNumber" text NOT NULL,
        "AppointmentId" uuid,
        "CustomerId" uuid,
        "Status" text NOT NULL,
        "Source" text NOT NULL,
        "CurrencyCode" text NOT NULL,
        "BusinessDate" date NOT NULL,
        "Subtotal" numeric(18,2) NOT NULL,
        "DiscountTotal" numeric(18,2) NOT NULL,
        "TaxableTotal" numeric(18,2) NOT NULL,
        "TaxTotal" numeric(18,2) NOT NULL,
        "TipTotal" numeric(18,2) NOT NULL,
        "GrandTotal" numeric(18,2) NOT NULL,
        "PaidTotal" numeric(18,2) NOT NULL,
        "BalanceDue" numeric(18,2) NOT NULL,
        "ChangeGiven" numeric(18,2) NOT NULL,
        "RoundingAdjustment" numeric(18,2) NOT NULL,
        "CustomerSnapshotJson" text,
        "CreatedByUserId" uuid,
        "PostedByUserId" uuid,
        "PostedAtUtc" timestamp with time zone,
        "PostingIdempotencyKey" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_Sales" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "TaxCodes" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "RatePercentage" numeric(18,2) NOT NULL,
        "TaxType" text NOT NULL,
        "IsInclusive" boolean NOT NULL,
        "IsDefault" boolean NOT NULL,
        "EffectiveFrom" date NOT NULL,
        "EffectiveTo" date,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TaxCodes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE TABLE "TillSessions" (
        "Id" uuid NOT NULL,
        "OrganizationId" uuid NOT NULL,
        "BranchId" uuid NOT NULL,
        "OpenedByUserId" uuid NOT NULL,
        "ClosedByUserId" uuid,
        "Status" text NOT NULL,
        "OpeningFloat" numeric(18,2) NOT NULL,
        "ExpectedCash" numeric(18,2) NOT NULL,
        "CountedCash" numeric(18,2),
        "Variance" numeric(18,2),
        "OpenedAtUtc" timestamp with time zone NOT NULL,
        "ClosedAtUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        "TenantId" uuid NOT NULL,
        CONSTRAINT "PK_TillSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_BranchProducts_TenantId_BranchId_ProductId" ON "BranchProducts" ("TenantId", "BranchId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_Invoices_TenantId_OrganizationId_InvoiceNumber" ON "Invoices" ("TenantId", "OrganizationId", "InvoiceNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_Invoices_TenantId_SaleId" ON "Invoices" ("TenantId", "SaleId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_OrganizationCommercialSettings_TenantId_OrganizationId" ON "OrganizationCommercialSettings" ("TenantId", "OrganizationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_Payments_TenantId_IdempotencyKey" ON "Payments" ("TenantId", "IdempotencyKey");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_Payments_TenantId_OrganizationId_PaymentNumber" ON "Payments" ("TenantId", "OrganizationId", "PaymentNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_Products_TenantId_OrganizationId_Barcode" ON "Products" ("TenantId", "OrganizationId", "Barcode") WHERE "Barcode" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_Products_TenantId_OrganizationId_Sku" ON "Products" ("TenantId", "OrganizationId", "Sku");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_Sales_TenantId_AppointmentId" ON "Sales" ("TenantId", "AppointmentId") WHERE "AppointmentId" IS NOT NULL AND "Status" <> 'Voided';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_Sales_TenantId_OrganizationId_SaleNumber" ON "Sales" ("TenantId", "OrganizationId", "SaleNumber");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_Sales_TenantId_PostingIdempotencyKey" ON "Sales" ("TenantId", "PostingIdempotencyKey") WHERE "PostingIdempotencyKey" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE UNIQUE INDEX "IX_TaxCodes_TenantId_OrganizationId_Code" ON "TaxCodes" ("TenantId", "OrganizationId", "Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    CREATE INDEX "IX_TillSessions_TenantId_BranchId_Status" ON "TillSessions" ("TenantId", "BranchId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260730043151_CommercialEngineFoundation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260730043151_CommercialEngineFoundation', '10.0.0');
    END IF;
END $EF$;
COMMIT;

