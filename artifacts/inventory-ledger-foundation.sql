START TRANSACTION;
ALTER TABLE "Products" ADD "AverageCost" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Products" ADD "BaseUnitOfMeasureId" uuid;

ALTER TABLE "Products" ADD "IsConsumable" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Products" ADD "LastPurchaseCost" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Products" ADD "MaximumStockLevel" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Products" ADD "MinimumStockLevel" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Products" ADD "ProductType" text NOT NULL DEFAULT '';

ALTER TABLE "Products" ADD "PurchaseUnitOfMeasureId" uuid;

ALTER TABLE "Products" ADD "ReorderPoint" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Products" ADD "ReorderQuantity" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Products" ADD "SaleUnitOfMeasureId" uuid;

ALTER TABLE "Products" ADD "StandardCost" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Products" ADD "TrackBatches" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Products" ADD "TrackExpiry" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "Products" ADD "TrackSerialNumbers" boolean NOT NULL DEFAULT FALSE;

CREATE TABLE "InventoryBalances" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "InventoryLocationId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "BatchId" uuid,
    "QuantityOnHand" numeric(18,2) NOT NULL,
    "QuantityReserved" numeric(18,2) NOT NULL,
    "AverageUnitCost" numeric(18,2) NOT NULL,
    "LastMovementAtUtc" timestamp with time zone NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_InventoryBalances" PRIMARY KEY ("Id")
);

CREATE TABLE "InventoryCostSettings" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "CostingMethod" text NOT NULL,
    "AllowNegativeStock" boolean NOT NULL,
    "NegativeStockCostPolicy" text NOT NULL,
    "CostRoundingPrecision" integer NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_InventoryCostSettings" PRIMARY KEY ("Id")
);

CREATE TABLE "InventoryLocations" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "Name" text NOT NULL,
    "Code" text NOT NULL,
    "LocationType" text NOT NULL,
    "ParentLocationId" uuid,
    "IsSellable" boolean NOT NULL,
    "IsConsumable" boolean NOT NULL,
    "IsQuarantine" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_InventoryLocations" PRIMARY KEY ("Id")
);

CREATE TABLE "ProductBatches" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "BatchNumber" text NOT NULL,
    "SupplierId" uuid,
    "ManufacturedAtUtc" timestamp with time zone,
    "ExpiresAtUtc" timestamp with time zone,
    "ReceivedAtUtc" timestamp with time zone NOT NULL,
    "InitialQuantity" numeric(18,2) NOT NULL,
    "RemainingQuantity" numeric(18,2) NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "Status" text NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_ProductBatches" PRIMARY KEY ("Id")
);

CREATE TABLE "ProductUnitConversions" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "FromUnitOfMeasureId" uuid NOT NULL,
    "ToUnitOfMeasureId" uuid NOT NULL,
    "ConversionFactor" numeric(18,2) NOT NULL,
    "IsPurchaseConversion" boolean NOT NULL,
    "IsSaleConversion" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_ProductUnitConversions" PRIMARY KEY ("Id")
);

CREATE TABLE "StockMovements" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "BranchId" uuid NOT NULL,
    "InventoryLocationId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "BatchId" uuid,
    "MovementType" text NOT NULL,
    "Direction" text NOT NULL,
    "QuantityBaseUnit" numeric(18,2) NOT NULL,
    "UnitCost" numeric(18,2) NOT NULL,
    "TotalCost" numeric(18,2) NOT NULL,
    "CurrencyCode" text NOT NULL,
    "ReferenceType" text NOT NULL,
    "ReferenceId" uuid,
    "ReferenceNumber" text,
    "ReasonCode" text NOT NULL,
    "Notes" text,
    "OccurredAtUtc" timestamp with time zone NOT NULL,
    "BusinessDate" date NOT NULL,
    "CreatedByUserId" uuid NOT NULL,
    "IdempotencyKey" text NOT NULL,
    "CorrelationId" text NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_StockMovements" PRIMARY KEY ("Id")
);

CREATE TABLE "UnitsOfMeasure" (
    "Id" uuid NOT NULL,
    "OrganizationId" uuid NOT NULL,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "UnitType" text NOT NULL,
    "DecimalPrecision" integer NOT NULL,
    "IsSystem" boolean NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NOT NULL,
    "TenantId" uuid NOT NULL,
    CONSTRAINT "PK_UnitsOfMeasure" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_InventoryBalances_TenantId_BranchId_InventoryLocationId_Pro~" ON "InventoryBalances" ("TenantId", "BranchId", "InventoryLocationId", "ProductId", "BatchId");

CREATE UNIQUE INDEX "IX_InventoryCostSettings_TenantId_OrganizationId" ON "InventoryCostSettings" ("TenantId", "OrganizationId");

CREATE UNIQUE INDEX "IX_InventoryLocations_TenantId_BranchId_Code" ON "InventoryLocations" ("TenantId", "BranchId", "Code");

CREATE UNIQUE INDEX "IX_ProductBatches_TenantId_BranchId_ProductId_BatchNumber" ON "ProductBatches" ("TenantId", "BranchId", "ProductId", "BatchNumber");

CREATE UNIQUE INDEX "IX_ProductUnitConversions_TenantId_ProductId_FromUnitOfMeasure~" ON "ProductUnitConversions" ("TenantId", "ProductId", "FromUnitOfMeasureId", "ToUnitOfMeasureId");

CREATE INDEX "IX_StockMovements_TenantId_BranchId_ProductId_OccurredAtUtc" ON "StockMovements" ("TenantId", "BranchId", "ProductId", "OccurredAtUtc");

CREATE UNIQUE INDEX "IX_StockMovements_TenantId_IdempotencyKey" ON "StockMovements" ("TenantId", "IdempotencyKey");

CREATE UNIQUE INDEX "IX_UnitsOfMeasure_TenantId_OrganizationId_Code" ON "UnitsOfMeasure" ("TenantId", "OrganizationId", "Code");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260730060504_InventoryLedgerFoundation', '10.0.0');

COMMIT;

