IF TYPE_ID(N'dbo.InventoryLineInputType') IS NULL
BEGIN
    CREATE TYPE dbo.InventoryLineInputType AS TABLE
    (
        ReferenceLineId BIGINT NULL,
        StockBatchId BIGINT NULL,
        MedicineMasterId BIGINT NULL,
        StockItemMasterId BIGINT NULL,
        ItemName NVARCHAR(180) NULL,
        UnitName NVARCHAR(40) NULL,
        BatchNumber NVARCHAR(60) NULL,
        ExpiryDate DATE NULL,
        Quantity DECIMAL(18, 2) NOT NULL,
        UnitCost DECIMAL(18, 2) NOT NULL,
        Reason NVARCHAR(200) NULL,
        Notes NVARCHAR(250) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.InventoryUnits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryUnits
    (
        InventoryUnitId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(80) NOT NULL,
        ShortName NVARCHAR(30) NULL,
        Description NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_InventoryUnits_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_InventoryUnits_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.InventoryCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryCategories
    (
        InventoryCategoryId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CategoryType NVARCHAR(20) NOT NULL,
        Name NVARCHAR(120) NOT NULL,
        Description NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_InventoryCategories_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_InventoryCategories_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.MedicineMasters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MedicineMasters
    (
        MedicineMasterId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InventoryUnitId BIGINT NOT NULL,
        InventoryCategoryId BIGINT NULL,
        MedicineName NVARCHAR(150) NOT NULL,
        GenericName NVARCHAR(150) NULL,
        Brand NVARCHAR(120) NULL,
        Strength NVARCHAR(80) NULL,
        BatchRequired BIT NOT NULL CONSTRAINT DF_MedicineMasters_BatchRequired DEFAULT (0),
        MinimumStock DECIMAL(18, 2) NOT NULL CONSTRAINT DF_MedicineMasters_MinimumStock DEFAULT (0),
        MaximumStock DECIMAL(18, 2) NOT NULL CONSTRAINT DF_MedicineMasters_MaximumStock DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_MedicineMasters_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MedicineMasters_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.StockItemMasters', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockItemMasters
    (
        StockItemMasterId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InventoryUnitId BIGINT NOT NULL,
        InventoryCategoryId BIGINT NULL,
        ItemType NVARCHAR(30) NOT NULL,
        ItemName NVARCHAR(150) NOT NULL,
        Specification NVARCHAR(150) NULL,
        MinimumStock DECIMAL(18, 2) NOT NULL CONSTRAINT DF_StockItemMasters_MinimumStock DEFAULT (0),
        MaximumStock DECIMAL(18, 2) NOT NULL CONSTRAINT DF_StockItemMasters_MaximumStock DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_StockItemMasters_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StockItemMasters_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Suppliers
    (
        SupplierId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SupplierName NVARCHAR(150) NOT NULL,
        SupplierCode NVARCHAR(40) NOT NULL,
        ContactPerson NVARCHAR(120) NULL,
        ContactPhone NVARCHAR(30) NULL,
        ContactEmail NVARCHAR(150) NULL,
        Address NVARCHAR(300) NULL,
        PaymentTermsDays INT NOT NULL CONSTRAINT DF_Suppliers_PaymentTermsDays DEFAULT (0),
        Notes NVARCHAR(400) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.StockLocations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockLocations
    (
        StockLocationId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BranchId BIGINT NULL,
        Name NVARCHAR(120) NOT NULL,
        Code NVARCHAR(40) NOT NULL,
        LocationType NVARCHAR(30) NOT NULL,
        Description NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_StockLocations_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StockLocations_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.PurchaseOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrders
    (
        PurchaseOrderId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        OrderNumber NVARCHAR(30) NOT NULL,
        SupplierId BIGINT NOT NULL,
        StockLocationId BIGINT NOT NULL,
        OrderDate DATE NOT NULL,
        ExpectedDeliveryDate DATE NULL,
        Status NVARCHAR(30) NOT NULL,
        Notes NVARCHAR(400) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PurchaseOrders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.PurchaseOrderLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrderLines
    (
        PurchaseOrderLineId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PurchaseOrderId BIGINT NOT NULL,
        MedicineMasterId BIGINT NULL,
        StockItemMasterId BIGINT NULL,
        ItemName NVARCHAR(180) NOT NULL,
        UnitName NVARCHAR(40) NULL,
        OrderedQuantity DECIMAL(18, 2) NOT NULL,
        ReceivedQuantity DECIMAL(18, 2) NOT NULL CONSTRAINT DF_PurchaseOrderLines_ReceivedQuantity DEFAULT (0),
        UnitCost DECIMAL(18, 2) NOT NULL CONSTRAINT DF_PurchaseOrderLines_UnitCost DEFAULT (0),
        Notes NVARCHAR(250) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PurchaseOrderLines_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.PurchaseInvoices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseInvoices
    (
        PurchaseInvoiceId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PurchaseOrderId BIGINT NOT NULL,
        SupplierId BIGINT NOT NULL,
        StockLocationId BIGINT NOT NULL,
        InvoiceNumber NVARCHAR(50) NOT NULL,
        InvoiceDate DATE NOT NULL,
        DueDate DATE NULL,
        TotalAmount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_PurchaseInvoices_TotalAmount DEFAULT (0),
        PaidAmount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_PurchaseInvoices_PaidAmount DEFAULT (0),
        Status NVARCHAR(30) NOT NULL,
        Notes NVARCHAR(400) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PurchaseInvoices_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.PurchaseInvoiceLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseInvoiceLines
    (
        PurchaseInvoiceLineId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PurchaseInvoiceId BIGINT NOT NULL,
        PurchaseOrderLineId BIGINT NULL,
        MedicineMasterId BIGINT NULL,
        StockItemMasterId BIGINT NULL,
        ItemName NVARCHAR(180) NOT NULL,
        UnitName NVARCHAR(40) NULL,
        BatchNumber NVARCHAR(60) NULL,
        ExpiryDate DATE NULL,
        Quantity DECIMAL(18, 2) NOT NULL,
        UnitCost DECIMAL(18, 2) NOT NULL,
        LineTotal DECIMAL(18, 2) NOT NULL,
        Notes NVARCHAR(250) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PurchaseInvoiceLines_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.PurchaseReturns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseReturns
    (
        PurchaseReturnId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PurchaseInvoiceId BIGINT NOT NULL,
        SupplierId BIGINT NOT NULL,
        ReturnNumber NVARCHAR(30) NOT NULL,
        ReturnDate DATE NOT NULL,
        TotalAmount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_PurchaseReturns_TotalAmount DEFAULT (0),
        Notes NVARCHAR(400) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PurchaseReturns_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.PurchaseReturnLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseReturnLines
    (
        PurchaseReturnLineId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PurchaseReturnId BIGINT NOT NULL,
        PurchaseInvoiceLineId BIGINT NULL,
        MedicineMasterId BIGINT NULL,
        StockItemMasterId BIGINT NULL,
        ItemName NVARCHAR(180) NOT NULL,
        BatchNumber NVARCHAR(60) NULL,
        ExpiryDate DATE NULL,
        Quantity DECIMAL(18, 2) NOT NULL,
        UnitCost DECIMAL(18, 2) NOT NULL,
        LineTotal DECIMAL(18, 2) NOT NULL,
        Reason NVARCHAR(200) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PurchaseReturnLines_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.StockBatches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockBatches
    (
        StockBatchId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StockLocationId BIGINT NOT NULL,
        MedicineMasterId BIGINT NULL,
        StockItemMasterId BIGINT NULL,
        BatchNumber NVARCHAR(60) NULL,
        ExpiryDate DATE NULL,
        QuantityOnHand DECIMAL(18, 2) NOT NULL CONSTRAINT DF_StockBatches_QuantityOnHand DEFAULT (0),
        UnitCost DECIMAL(18, 2) NOT NULL CONSTRAINT DF_StockBatches_UnitCost DEFAULT (0),
        LastMovementAt DATETIME2 NOT NULL CONSTRAINT DF_StockBatches_LastMovementAt DEFAULT (SYSUTCDATETIME()),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StockBatches_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.StockTransfers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockTransfers
    (
        StockTransferId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TransferNumber NVARCHAR(30) NOT NULL,
        FromStockLocationId BIGINT NOT NULL,
        ToStockLocationId BIGINT NOT NULL,
        TransferDate DATE NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        Notes NVARCHAR(400) NULL,
        ApprovedAt DATETIME2 NULL,
        ReceivedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StockTransfers_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.StockTransferLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockTransferLines
    (
        StockTransferLineId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StockTransferId BIGINT NOT NULL,
        StockBatchId BIGINT NOT NULL,
        ItemName NVARCHAR(180) NOT NULL,
        BatchNumber NVARCHAR(60) NULL,
        ExpiryDate DATE NULL,
        Quantity DECIMAL(18, 2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StockTransferLines_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.StockAdjustments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockAdjustments
    (
        StockAdjustmentId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AdjustmentNumber NVARCHAR(30) NOT NULL,
        StockLocationId BIGINT NOT NULL,
        Reason NVARCHAR(30) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        AdjustmentDate DATE NOT NULL,
        Notes NVARCHAR(400) NULL,
        ApprovedAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StockAdjustments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.StockAdjustmentLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockAdjustmentLines
    (
        StockAdjustmentLineId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        StockAdjustmentId BIGINT NOT NULL,
        StockBatchId BIGINT NOT NULL,
        ItemName NVARCHAR(180) NOT NULL,
        BatchNumber NVARCHAR(60) NULL,
        ExpiryDate DATE NULL,
        QuantityDelta DECIMAL(18, 2) NOT NULL,
        Notes NVARCHAR(250) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_StockAdjustmentLines_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventoryUnits') AND name = N'IX_InventoryUnits_Name')
    CREATE UNIQUE INDEX IX_InventoryUnits_Name ON dbo.InventoryUnits(Name);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventoryCategories') AND name = N'IX_InventoryCategories_TypeName')
    CREATE UNIQUE INDEX IX_InventoryCategories_TypeName ON dbo.InventoryCategories(CategoryType, Name);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Suppliers') AND name = N'IX_Suppliers_SupplierCode')
    CREATE UNIQUE INDEX IX_Suppliers_SupplierCode ON dbo.Suppliers(SupplierCode);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.StockLocations') AND name = N'IX_StockLocations_Code')
    CREATE UNIQUE INDEX IX_StockLocations_Code ON dbo.StockLocations(Code);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PurchaseOrders') AND name = N'IX_PurchaseOrders_OrderNumber')
    CREATE UNIQUE INDEX IX_PurchaseOrders_OrderNumber ON dbo.PurchaseOrders(OrderNumber);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PurchaseInvoices') AND name = N'IX_PurchaseInvoices_SupplierInvoice')
    CREATE UNIQUE INDEX IX_PurchaseInvoices_SupplierInvoice ON dbo.PurchaseInvoices(SupplierId, InvoiceNumber);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PurchaseReturns') AND name = N'IX_PurchaseReturns_ReturnNumber')
    CREATE UNIQUE INDEX IX_PurchaseReturns_ReturnNumber ON dbo.PurchaseReturns(ReturnNumber);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.StockTransfers') AND name = N'IX_StockTransfers_TransferNumber')
    CREATE UNIQUE INDEX IX_StockTransfers_TransferNumber ON dbo.StockTransfers(TransferNumber);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.StockAdjustments') AND name = N'IX_StockAdjustments_AdjustmentNumber')
    CREATE UNIQUE INDEX IX_StockAdjustments_AdjustmentNumber ON dbo.StockAdjustments(AdjustmentNumber);
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetUnits
AS
BEGIN
    SET NOCOUNT ON;

    SELECT InventoryUnitId, Name, ShortName, Description, IsActive
    FROM dbo.InventoryUnits
    ORDER BY Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetCategories
AS
BEGIN
    SET NOCOUNT ON;

    SELECT InventoryCategoryId, CategoryType, Name, Description, IsActive
    FROM dbo.InventoryCategories
    ORDER BY CategoryType, Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetMedicines
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.MedicineMasterId,
        m.MedicineName,
        m.GenericName,
        m.Brand,
        u.Name AS UnitName,
        m.InventoryUnitId,
        c.Name AS CategoryName,
        m.InventoryCategoryId,
        m.Strength,
        m.BatchRequired,
        m.MinimumStock,
        m.MaximumStock,
        m.IsActive
    FROM dbo.MedicineMasters m
    INNER JOIN dbo.InventoryUnits u ON u.InventoryUnitId = m.InventoryUnitId
    LEFT JOIN dbo.InventoryCategories c ON c.InventoryCategoryId = m.InventoryCategoryId
    ORDER BY m.MedicineName, m.Brand, m.Strength;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetItems
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        i.StockItemMasterId,
        i.ItemType,
        i.ItemName,
        i.Specification,
        u.Name AS UnitName,
        i.InventoryUnitId,
        c.Name AS CategoryName,
        i.InventoryCategoryId,
        i.MinimumStock,
        i.MaximumStock,
        i.IsActive
    FROM dbo.StockItemMasters i
    INNER JOIN dbo.InventoryUnits u ON u.InventoryUnitId = i.InventoryUnitId
    LEFT JOIN dbo.InventoryCategories c ON c.InventoryCategoryId = i.InventoryCategoryId
    ORDER BY i.ItemType, i.ItemName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetLocations
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        l.StockLocationId,
        l.BranchId,
        ISNULL(b.Name, N'Unassigned') AS BranchName,
        l.Name,
        l.Code,
        l.LocationType,
        l.Description,
        l.IsActive
    FROM dbo.StockLocations l
    LEFT JOIN dbo.Branches b ON b.BranchId = l.BranchId
    ORDER BY l.LocationType, l.Name;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetSupplierDueSummary
AS
BEGIN
    SET NOCOUNT ON;

    WITH ReturnAmounts AS
    (
        SELECT pi.SupplierId, SUM(pr.TotalAmount) AS ReturnAmount
        FROM dbo.PurchaseReturns pr
        INNER JOIN dbo.PurchaseInvoices pi ON pi.PurchaseInvoiceId = pr.PurchaseInvoiceId
        GROUP BY pi.SupplierId
    ),
    InvoiceTotals AS
    (
        SELECT
            pi.SupplierId,
            COUNT(CASE WHEN pi.Status <> N'Cancelled' AND (pi.TotalAmount - pi.PaidAmount) > 0 THEN 1 END) AS OpenInvoiceCount,
            SUM(CASE WHEN pi.Status <> N'Cancelled' THEN pi.TotalAmount ELSE 0 END) AS TotalInvoiceAmount,
            SUM(CASE WHEN pi.Status <> N'Cancelled' THEN pi.PaidAmount ELSE 0 END) AS PaidAmount
        FROM dbo.PurchaseInvoices pi
        GROUP BY pi.SupplierId
    )
    SELECT
        s.SupplierId,
        s.SupplierName,
        s.SupplierCode,
        ISNULL(i.OpenInvoiceCount, 0) AS OpenInvoiceCount,
        ISNULL(i.TotalInvoiceAmount, 0) AS TotalInvoiceAmount,
        ISNULL(i.PaidAmount, 0) AS PaidAmount,
        ISNULL(r.ReturnAmount, 0) AS ReturnAmount,
        ISNULL(i.TotalInvoiceAmount, 0) - ISNULL(i.PaidAmount, 0) - ISNULL(r.ReturnAmount, 0) AS DueAmount
    FROM dbo.Suppliers s
    LEFT JOIN InvoiceTotals i ON i.SupplierId = s.SupplierId
    LEFT JOIN ReturnAmounts r ON r.SupplierId = s.SupplierId
    ORDER BY DueAmount DESC, s.SupplierName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetSuppliers
AS
BEGIN
    SET NOCOUNT ON;

    WITH PurchaseOrderCounts AS
    (
        SELECT SupplierId, COUNT(*) AS PurchaseOrderCount
        FROM dbo.PurchaseOrders
        GROUP BY SupplierId
    ),
    PurchaseInvoiceStats AS
    (
        SELECT
            SupplierId,
            COUNT(*) AS PurchaseInvoiceCount,
            SUM(CASE WHEN Status <> N'Cancelled' THEN TotalAmount ELSE 0 END) AS TotalPurchasedAmount,
            MAX(InvoiceDate) AS LastPurchaseDate
        FROM dbo.PurchaseInvoices
        GROUP BY SupplierId
    ),
    ReturnAmounts AS
    (
        SELECT pi.SupplierId, SUM(pr.TotalAmount) AS ReturnAmount
        FROM dbo.PurchaseReturns pr
        INNER JOIN dbo.PurchaseInvoices pi ON pi.PurchaseInvoiceId = pr.PurchaseInvoiceId
        GROUP BY pi.SupplierId
    ),
    DueSummary AS
    (
        SELECT
            pi.SupplierId,
            SUM(CASE WHEN pi.Status <> N'Cancelled' THEN pi.TotalAmount ELSE 0 END) AS TotalInvoiceAmount,
            SUM(CASE WHEN pi.Status <> N'Cancelled' THEN pi.PaidAmount ELSE 0 END) AS PaidAmount
        FROM dbo.PurchaseInvoices pi
        GROUP BY pi.SupplierId
    )
    SELECT
        s.SupplierId,
        s.SupplierName,
        s.SupplierCode,
        s.ContactPerson,
        s.ContactPhone,
        s.ContactEmail,
        s.Address,
        s.PaymentTermsDays,
        s.Notes,
        s.IsActive,
        ISNULL(po.PurchaseOrderCount, 0) AS PurchaseOrderCount,
        ISNULL(pi.PurchaseInvoiceCount, 0) AS PurchaseInvoiceCount,
        ISNULL(pi.TotalPurchasedAmount, 0) AS TotalPurchasedAmount,
        ISNULL(d.TotalInvoiceAmount, 0) - ISNULL(d.PaidAmount, 0) - ISNULL(r.ReturnAmount, 0) AS DueAmount,
        pi.LastPurchaseDate
    FROM dbo.Suppliers s
    LEFT JOIN PurchaseOrderCounts po ON po.SupplierId = s.SupplierId
    LEFT JOIN PurchaseInvoiceStats pi ON pi.SupplierId = s.SupplierId
    LEFT JOIN DueSummary d ON d.SupplierId = s.SupplierId
    LEFT JOIN ReturnAmounts r ON r.SupplierId = s.SupplierId
    ORDER BY s.SupplierName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetBatches
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    DECLARE @NearExpiry DATE = DATEADD(DAY, 30, @Today);

    SELECT
        sb.StockBatchId,
        sb.StockLocationId,
        l.Name AS LocationName,
        CASE WHEN sb.MedicineMasterId IS NOT NULL THEN N'Medicine' ELSE ISNULL(i.ItemType, N'Item') END AS ItemType,
        sb.MedicineMasterId,
        sb.StockItemMasterId,
        COALESCE(m.MedicineName, i.ItemName) AS ItemName,
        u.Name AS UnitName,
        c.Name AS CategoryName,
        sb.BatchNumber,
        sb.ExpiryDate,
        sb.QuantityOnHand,
        sb.UnitCost,
        COALESCE(m.MinimumStock, i.MinimumStock, 0) AS MinimumStock,
        COALESCE(m.MaximumStock, i.MaximumStock, 0) AS MaximumStock,
        CASE WHEN sb.ExpiryDate IS NOT NULL AND sb.ExpiryDate >= @Today AND sb.ExpiryDate <= @NearExpiry THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsNearExpiry,
        CASE WHEN sb.ExpiryDate IS NOT NULL AND sb.ExpiryDate < @Today THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsExpiredLocked
    FROM dbo.StockBatches sb
    INNER JOIN dbo.StockLocations l ON l.StockLocationId = sb.StockLocationId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = sb.MedicineMasterId
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = sb.StockItemMasterId
    LEFT JOIN dbo.InventoryUnits u ON u.InventoryUnitId = COALESCE(m.InventoryUnitId, i.InventoryUnitId)
    LEFT JOIN dbo.InventoryCategories c ON c.InventoryCategoryId = COALESCE(m.InventoryCategoryId, i.InventoryCategoryId)
    WHERE sb.QuantityOnHand <> 0
    ORDER BY l.Name, COALESCE(m.MedicineName, i.ItemName), sb.BatchNumber;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetDashboard
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today DATE = CAST(GETDATE() AS DATE);
    DECLARE @NearExpiry DATE = DATEADD(DAY, 30, @Today);

    CREATE TABLE #BatchView
    (
        StockBatchId BIGINT NOT NULL,
        LocationName NVARCHAR(120) NOT NULL,
        ItemType NVARCHAR(40) NOT NULL,
        ItemName NVARCHAR(200) NOT NULL,
        UnitName NVARCHAR(80) NULL,
        BatchNumber NVARCHAR(80) NULL,
        ExpiryDate DATE NULL,
        QuantityOnHand DECIMAL(18, 2) NOT NULL,
        MinimumStock DECIMAL(18, 2) NOT NULL,
        IsExpiredLocked BIT NOT NULL
    );

    INSERT INTO #BatchView
    (
        StockBatchId,
        LocationName,
        ItemType,
        ItemName,
        UnitName,
        BatchNumber,
        ExpiryDate,
        QuantityOnHand,
        MinimumStock,
        IsExpiredLocked
    )
    SELECT
        sb.StockBatchId,
        l.Name AS LocationName,
        CASE WHEN sb.MedicineMasterId IS NOT NULL THEN N'Medicine' ELSE ISNULL(i.ItemType, N'Item') END AS ItemType,
        COALESCE(m.MedicineName, i.ItemName) AS ItemName,
        u.Name AS UnitName,
        sb.BatchNumber,
        sb.ExpiryDate,
        sb.QuantityOnHand,
        COALESCE(m.MinimumStock, i.MinimumStock, 0) AS MinimumStock,
        CASE WHEN sb.ExpiryDate IS NOT NULL AND sb.ExpiryDate < @Today THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsExpiredLocked
    FROM dbo.StockBatches sb
    INNER JOIN dbo.StockLocations l ON l.StockLocationId = sb.StockLocationId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = sb.MedicineMasterId
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = sb.StockItemMasterId
    LEFT JOIN dbo.InventoryUnits u ON u.InventoryUnitId = COALESCE(m.InventoryUnitId, i.InventoryUnitId)
    WHERE sb.QuantityOnHand > 0;

    WITH DueSummary AS
    (
        SELECT
            SUM(CASE WHEN pi.Status <> N'Cancelled' THEN pi.TotalAmount ELSE 0 END)
            - SUM(CASE WHEN pi.Status <> N'Cancelled' THEN pi.PaidAmount ELSE 0 END)
            - ISNULL((SELECT SUM(pr.TotalAmount) FROM dbo.PurchaseReturns pr), 0) AS SupplierDueAmount
        FROM dbo.PurchaseInvoices pi
    )
    SELECT
        (SELECT COUNT(*) FROM dbo.StockLocations WHERE IsActive = 1) AS ActiveLocations,
        (SELECT COUNT(*) FROM dbo.Suppliers WHERE IsActive = 1) AS ActiveSuppliers,
        (SELECT COUNT(*) FROM dbo.PurchaseOrders WHERE Status IN (N'Draft', N'Ordered', N'PartiallyReceived')) AS OpenPurchaseOrders,
        (SELECT COUNT(*) FROM dbo.StockTransfers WHERE Status IN (N'Pending', N'Approved')) AS PendingTransfers,
        (SELECT COUNT(*) FROM dbo.StockAdjustments WHERE Status = N'Pending') AS PendingAdjustments,
        (SELECT COUNT(*) FROM #BatchView WHERE QuantityOnHand <= MinimumStock AND IsExpiredLocked = 0) AS LowStockCount,
        (SELECT COUNT(*) FROM #BatchView WHERE ExpiryDate IS NOT NULL AND ExpiryDate >= @Today AND ExpiryDate <= @NearExpiry) AS NearExpiryCount,
        (SELECT COUNT(*) FROM #BatchView WHERE IsExpiredLocked = 1) AS ExpiredCount,
        ISNULL((SELECT SupplierDueAmount FROM DueSummary), 0) AS SupplierDueAmount;

    SELECT TOP (6)
        StockBatchId,
        ItemType,
        ItemName,
        UnitName,
        LocationName,
        BatchNumber,
        ExpiryDate,
        QuantityOnHand,
        MinimumStock,
        IsExpiredLocked
    FROM #BatchView
    WHERE QuantityOnHand <= MinimumStock AND IsExpiredLocked = 0
    ORDER BY QuantityOnHand, ItemName;

    SELECT TOP (6)
        StockBatchId,
        ItemType,
        ItemName,
        UnitName,
        LocationName,
        BatchNumber,
        ExpiryDate,
        QuantityOnHand,
        MinimumStock,
        IsExpiredLocked
    FROM #BatchView
    WHERE ExpiryDate IS NOT NULL AND ExpiryDate >= @Today AND ExpiryDate <= @NearExpiry
    ORDER BY ExpiryDate, ItemName;

    EXEC dbo.usp_Inventory_GetSupplierDueSummary;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetPurchaseOrders
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        po.PurchaseOrderId,
        po.OrderNumber,
        po.SupplierId,
        s.SupplierName,
        po.StockLocationId,
        l.Name AS StockLocationName,
        po.OrderDate,
        po.ExpectedDeliveryDate,
        po.Status,
        po.Notes,
        ISNULL(SUM(pol.OrderedQuantity * pol.UnitCost), 0) AS TotalAmount,
        ISNULL(SUM(pol.OrderedQuantity), 0) AS OrderedQuantity,
        ISNULL(SUM(pol.ReceivedQuantity), 0) AS ReceivedQuantity
    FROM dbo.PurchaseOrders po
    INNER JOIN dbo.Suppliers s ON s.SupplierId = po.SupplierId
    INNER JOIN dbo.StockLocations l ON l.StockLocationId = po.StockLocationId
    LEFT JOIN dbo.PurchaseOrderLines pol ON pol.PurchaseOrderId = po.PurchaseOrderId
    GROUP BY po.PurchaseOrderId, po.OrderNumber, po.SupplierId, s.SupplierName, po.StockLocationId, l.Name, po.OrderDate, po.ExpectedDeliveryDate, po.Status, po.Notes
    ORDER BY po.OrderDate DESC, po.PurchaseOrderId DESC;

    SELECT
        pol.PurchaseOrderId AS ParentId,
        pol.PurchaseOrderLineId AS ReferenceLineId,
        CAST(NULL AS BIGINT) AS StockBatchId,
        pol.MedicineMasterId,
        pol.StockItemMasterId,
        CASE WHEN pol.MedicineMasterId IS NOT NULL THEN N'Medicine' ELSE ISNULL(i.ItemType, N'Item') END AS ItemType,
        pol.ItemName,
        ISNULL(pol.UnitName, u.Name) AS UnitName,
        CAST(NULL AS NVARCHAR(60)) AS BatchNumber,
        CAST(NULL AS DATE) AS ExpiryDate,
        pol.OrderedQuantity AS Quantity,
        pol.ReceivedQuantity,
        pol.UnitCost,
        pol.OrderedQuantity * pol.UnitCost AS LineTotal,
        CAST(NULL AS NVARCHAR(200)) AS Reason,
        pol.Notes
    FROM dbo.PurchaseOrderLines pol
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = pol.StockItemMasterId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = pol.MedicineMasterId
    LEFT JOIN dbo.InventoryUnits u ON u.InventoryUnitId = COALESCE(m.InventoryUnitId, i.InventoryUnitId)
    ORDER BY pol.PurchaseOrderId, pol.PurchaseOrderLineId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetPurchaseInvoices
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pi.PurchaseInvoiceId,
        pi.PurchaseOrderId,
        po.OrderNumber,
        pi.SupplierId,
        s.SupplierName,
        pi.StockLocationId,
        l.Name AS StockLocationName,
        pi.InvoiceNumber,
        pi.InvoiceDate,
        pi.DueDate,
        pi.TotalAmount,
        pi.PaidAmount,
        pi.TotalAmount - pi.PaidAmount - ISNULL((SELECT SUM(pr.TotalAmount) FROM dbo.PurchaseReturns pr WHERE pr.PurchaseInvoiceId = pi.PurchaseInvoiceId), 0) AS DueAmount,
        pi.Status,
        pi.Notes
    FROM dbo.PurchaseInvoices pi
    INNER JOIN dbo.Suppliers s ON s.SupplierId = pi.SupplierId
    INNER JOIN dbo.StockLocations l ON l.StockLocationId = pi.StockLocationId
    INNER JOIN dbo.PurchaseOrders po ON po.PurchaseOrderId = pi.PurchaseOrderId
    ORDER BY pi.InvoiceDate DESC, pi.PurchaseInvoiceId DESC;

    SELECT
        pil.PurchaseInvoiceId AS ParentId,
        pil.PurchaseInvoiceLineId AS ReferenceLineId,
        CAST(NULL AS BIGINT) AS StockBatchId,
        pil.MedicineMasterId,
        pil.StockItemMasterId,
        CASE WHEN pil.MedicineMasterId IS NOT NULL THEN N'Medicine' ELSE ISNULL(i.ItemType, N'Item') END AS ItemType,
        pil.ItemName,
        ISNULL(pil.UnitName, u.Name) AS UnitName,
        pil.BatchNumber,
        pil.ExpiryDate,
        pil.Quantity,
        pil.Quantity AS ReceivedQuantity,
        pil.UnitCost,
        pil.LineTotal,
        CAST(NULL AS NVARCHAR(200)) AS Reason,
        pil.Notes
    FROM dbo.PurchaseInvoiceLines pil
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = pil.StockItemMasterId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = pil.MedicineMasterId
    LEFT JOIN dbo.InventoryUnits u ON u.InventoryUnitId = COALESCE(m.InventoryUnitId, i.InventoryUnitId)
    ORDER BY pil.PurchaseInvoiceId, pil.PurchaseInvoiceLineId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetPurchaseReturns
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        pr.PurchaseReturnId,
        pr.PurchaseInvoiceId,
        pi.InvoiceNumber,
        pr.SupplierId,
        s.SupplierName,
        pr.ReturnNumber,
        pr.ReturnDate,
        pr.TotalAmount,
        pr.Notes
    FROM dbo.PurchaseReturns pr
    INNER JOIN dbo.PurchaseInvoices pi ON pi.PurchaseInvoiceId = pr.PurchaseInvoiceId
    INNER JOIN dbo.Suppliers s ON s.SupplierId = pr.SupplierId
    ORDER BY pr.ReturnDate DESC, pr.PurchaseReturnId DESC;

    SELECT
        prl.PurchaseReturnId AS ParentId,
        prl.PurchaseReturnLineId AS ReferenceLineId,
        CAST(NULL AS BIGINT) AS StockBatchId,
        prl.MedicineMasterId,
        prl.StockItemMasterId,
        CASE WHEN prl.MedicineMasterId IS NOT NULL THEN N'Medicine' ELSE ISNULL(i.ItemType, N'Item') END AS ItemType,
        prl.ItemName,
        u.Name AS UnitName,
        prl.BatchNumber,
        prl.ExpiryDate,
        prl.Quantity,
        CAST(0 AS DECIMAL(18,2)) AS ReceivedQuantity,
        prl.UnitCost,
        prl.LineTotal,
        prl.Reason,
        CAST(NULL AS NVARCHAR(250)) AS Notes
    FROM dbo.PurchaseReturnLines prl
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = prl.StockItemMasterId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = prl.MedicineMasterId
    LEFT JOIN dbo.InventoryUnits u ON u.InventoryUnitId = COALESCE(m.InventoryUnitId, i.InventoryUnitId)
    ORDER BY prl.PurchaseReturnId, prl.PurchaseReturnLineId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetTransfers
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        st.StockTransferId,
        st.TransferNumber,
        st.FromStockLocationId,
        fl.Name AS FromStockLocationName,
        st.ToStockLocationId,
        tl.Name AS ToStockLocationName,
        st.TransferDate,
        st.Status,
        st.Notes,
        st.ApprovedAt,
        st.ReceivedAt,
        ISNULL(SUM(stl.Quantity), 0) AS TotalQuantity
    FROM dbo.StockTransfers st
    INNER JOIN dbo.StockLocations fl ON fl.StockLocationId = st.FromStockLocationId
    INNER JOIN dbo.StockLocations tl ON tl.StockLocationId = st.ToStockLocationId
    LEFT JOIN dbo.StockTransferLines stl ON stl.StockTransferId = st.StockTransferId
    GROUP BY st.StockTransferId, st.TransferNumber, st.FromStockLocationId, fl.Name, st.ToStockLocationId, tl.Name, st.TransferDate, st.Status, st.Notes, st.ApprovedAt, st.ReceivedAt
    ORDER BY st.TransferDate DESC, st.StockTransferId DESC;

    SELECT
        stl.StockTransferId AS ParentId,
        stl.StockTransferLineId AS ReferenceLineId,
        stl.StockBatchId,
        sb.MedicineMasterId,
        sb.StockItemMasterId,
        CASE WHEN sb.MedicineMasterId IS NOT NULL THEN N'Medicine' ELSE ISNULL(i.ItemType, N'Item') END AS ItemType,
        stl.ItemName,
        u.Name AS UnitName,
        stl.BatchNumber,
        stl.ExpiryDate,
        stl.Quantity,
        CAST(0 AS DECIMAL(18,2)) AS ReceivedQuantity,
        sb.UnitCost,
        stl.Quantity * sb.UnitCost AS LineTotal,
        CAST(NULL AS NVARCHAR(200)) AS Reason,
        CAST(NULL AS NVARCHAR(250)) AS Notes
    FROM dbo.StockTransferLines stl
    INNER JOIN dbo.StockBatches sb ON sb.StockBatchId = stl.StockBatchId
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = sb.StockItemMasterId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = sb.MedicineMasterId
    LEFT JOIN dbo.InventoryUnits u ON u.InventoryUnitId = COALESCE(m.InventoryUnitId, i.InventoryUnitId)
    ORDER BY stl.StockTransferId, stl.StockTransferLineId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_GetAdjustments
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        sa.StockAdjustmentId,
        sa.AdjustmentNumber,
        sa.StockLocationId,
        l.Name AS StockLocationName,
        sa.Reason,
        sa.Status,
        sa.AdjustmentDate,
        sa.Notes,
        sa.ApprovedAt,
        ISNULL(SUM(sal.QuantityDelta), 0) AS TotalDelta
    FROM dbo.StockAdjustments sa
    INNER JOIN dbo.StockLocations l ON l.StockLocationId = sa.StockLocationId
    LEFT JOIN dbo.StockAdjustmentLines sal ON sal.StockAdjustmentId = sa.StockAdjustmentId
    GROUP BY sa.StockAdjustmentId, sa.AdjustmentNumber, sa.StockLocationId, l.Name, sa.Reason, sa.Status, sa.AdjustmentDate, sa.Notes, sa.ApprovedAt
    ORDER BY sa.AdjustmentDate DESC, sa.StockAdjustmentId DESC;

    SELECT
        sal.StockAdjustmentId AS ParentId,
        sal.StockAdjustmentLineId AS ReferenceLineId,
        sal.StockBatchId,
        sb.MedicineMasterId,
        sb.StockItemMasterId,
        CASE WHEN sb.MedicineMasterId IS NOT NULL THEN N'Medicine' ELSE ISNULL(i.ItemType, N'Item') END AS ItemType,
        sal.ItemName,
        u.Name AS UnitName,
        sal.BatchNumber,
        sal.ExpiryDate,
        sal.QuantityDelta AS Quantity,
        CAST(0 AS DECIMAL(18,2)) AS ReceivedQuantity,
        sb.UnitCost,
        sal.QuantityDelta * sb.UnitCost AS LineTotal,
        sa.Reason,
        sal.Notes
    FROM dbo.StockAdjustmentLines sal
    INNER JOIN dbo.StockAdjustments sa ON sa.StockAdjustmentId = sal.StockAdjustmentId
    INNER JOIN dbo.StockBatches sb ON sb.StockBatchId = sal.StockBatchId
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = sb.StockItemMasterId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = sb.MedicineMasterId
    LEFT JOIN dbo.InventoryUnits u ON u.InventoryUnitId = COALESCE(m.InventoryUnitId, i.InventoryUnitId)
    ORDER BY sal.StockAdjustmentId, sal.StockAdjustmentLineId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SaveUnit
    @InventoryUnitId BIGINT OUTPUT,
    @Name NVARCHAR(80),
    @ShortName NVARCHAR(30) = NULL,
    @Description NVARCHAR(250) = NULL,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Name = LTRIM(RTRIM(@Name));
    IF @Name = N'' THROW 50001, N'Unit name is required.', 1;

    IF EXISTS (SELECT 1 FROM dbo.InventoryUnits WHERE Name = @Name AND InventoryUnitId <> ISNULL(@InventoryUnitId, 0))
        THROW 50001, N'Inventory unit name already exists.', 1;

    IF ISNULL(@InventoryUnitId, 0) = 0
    BEGIN
        INSERT INTO dbo.InventoryUnits (Name, ShortName, Description, IsActive, CreatedAt)
        VALUES (@Name, NULLIF(LTRIM(RTRIM(@ShortName)), N''), NULLIF(LTRIM(RTRIM(@Description)), N''), @IsActive, SYSUTCDATETIME());
        SET @InventoryUnitId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.InventoryUnits
        SET Name = @Name,
            ShortName = NULLIF(LTRIM(RTRIM(@ShortName)), N''),
            Description = NULLIF(LTRIM(RTRIM(@Description)), N''),
            IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        WHERE InventoryUnitId = @InventoryUnitId;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SaveCategory
    @InventoryCategoryId BIGINT OUTPUT,
    @CategoryType NVARCHAR(20),
    @Name NVARCHAR(120),
    @Description NVARCHAR(250) = NULL,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    SET @CategoryType = LTRIM(RTRIM(@CategoryType));
    SET @Name = LTRIM(RTRIM(@Name));

    IF @Name = N'' THROW 50001, N'Category name is required.', 1;
    IF @CategoryType NOT IN (N'Medicine', N'Item') THROW 50001, N'Invalid category type.', 1;

    IF EXISTS (SELECT 1 FROM dbo.InventoryCategories WHERE CategoryType = @CategoryType AND Name = @Name AND InventoryCategoryId <> ISNULL(@InventoryCategoryId, 0))
        THROW 50001, N'Inventory category already exists.', 1;

    IF ISNULL(@InventoryCategoryId, 0) = 0
    BEGIN
        INSERT INTO dbo.InventoryCategories (CategoryType, Name, Description, IsActive, CreatedAt)
        VALUES (@CategoryType, @Name, NULLIF(LTRIM(RTRIM(@Description)), N''), @IsActive, SYSUTCDATETIME());
        SET @InventoryCategoryId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.InventoryCategories
        SET CategoryType = @CategoryType,
            Name = @Name,
            Description = NULLIF(LTRIM(RTRIM(@Description)), N''),
            IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        WHERE InventoryCategoryId = @InventoryCategoryId;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SaveMedicine
    @MedicineMasterId BIGINT OUTPUT,
    @MedicineName NVARCHAR(150),
    @GenericName NVARCHAR(150) = NULL,
    @Brand NVARCHAR(120) = NULL,
    @InventoryUnitId BIGINT,
    @InventoryCategoryId BIGINT = NULL,
    @Strength NVARCHAR(80) = NULL,
    @BatchRequired BIT,
    @MinimumStock DECIMAL(18, 2),
    @MaximumStock DECIMAL(18, 2),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    SET @MedicineName = LTRIM(RTRIM(@MedicineName));
    IF @MedicineName = N'' THROW 50001, N'Medicine name is required.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.InventoryUnits WHERE InventoryUnitId = @InventoryUnitId) THROW 50001, N'Unit not found.', 1;
    IF @InventoryCategoryId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.InventoryCategories WHERE InventoryCategoryId = @InventoryCategoryId AND CategoryType = N'Medicine')
        THROW 50001, N'Medicine category not found.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.MedicineMasters
        WHERE MedicineName = @MedicineName
          AND ISNULL(Brand, N'') = ISNULL(NULLIF(LTRIM(RTRIM(@Brand)), N''), N'')
          AND ISNULL(Strength, N'') = ISNULL(NULLIF(LTRIM(RTRIM(@Strength)), N''), N'')
          AND MedicineMasterId <> ISNULL(@MedicineMasterId, 0))
        THROW 50001, N'Medicine master already exists.', 1;

    IF ISNULL(@MedicineMasterId, 0) = 0
    BEGIN
        INSERT INTO dbo.MedicineMasters
        (
            InventoryUnitId, InventoryCategoryId, MedicineName, GenericName, Brand, Strength,
            BatchRequired, MinimumStock, MaximumStock, IsActive, CreatedAt
        )
        VALUES
        (
            @InventoryUnitId, @InventoryCategoryId, @MedicineName, NULLIF(LTRIM(RTRIM(@GenericName)), N''), NULLIF(LTRIM(RTRIM(@Brand)), N''), NULLIF(LTRIM(RTRIM(@Strength)), N''),
            @BatchRequired, @MinimumStock, @MaximumStock, @IsActive, SYSUTCDATETIME()
        );
        SET @MedicineMasterId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.MedicineMasters
        SET InventoryUnitId = @InventoryUnitId,
            InventoryCategoryId = @InventoryCategoryId,
            MedicineName = @MedicineName,
            GenericName = NULLIF(LTRIM(RTRIM(@GenericName)), N''),
            Brand = NULLIF(LTRIM(RTRIM(@Brand)), N''),
            Strength = NULLIF(LTRIM(RTRIM(@Strength)), N''),
            BatchRequired = @BatchRequired,
            MinimumStock = @MinimumStock,
            MaximumStock = @MaximumStock,
            IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        WHERE MedicineMasterId = @MedicineMasterId;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SaveItem
    @StockItemMasterId BIGINT OUTPUT,
    @ItemType NVARCHAR(30),
    @ItemName NVARCHAR(150),
    @Specification NVARCHAR(150) = NULL,
    @InventoryUnitId BIGINT,
    @InventoryCategoryId BIGINT = NULL,
    @MinimumStock DECIMAL(18, 2),
    @MaximumStock DECIMAL(18, 2),
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    SET @ItemType = LTRIM(RTRIM(@ItemType));
    SET @ItemName = LTRIM(RTRIM(@ItemName));
    IF @ItemName = N'' THROW 50001, N'Item name is required.', 1;
    IF @ItemType NOT IN (N'Consumable', N'SurgicalItem', N'LabReagent', N'NonMedicalSupply', N'EquipmentSpare')
        THROW 50001, N'Invalid item type.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.InventoryUnits WHERE InventoryUnitId = @InventoryUnitId) THROW 50001, N'Unit not found.', 1;
    IF @InventoryCategoryId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.InventoryCategories WHERE InventoryCategoryId = @InventoryCategoryId AND CategoryType = N'Item')
        THROW 50001, N'Item category not found.', 1;

    IF EXISTS (
        SELECT 1
        FROM dbo.StockItemMasters
        WHERE ItemType = @ItemType
          AND ItemName = @ItemName
          AND StockItemMasterId <> ISNULL(@StockItemMasterId, 0))
        THROW 50001, N'Stock item already exists.', 1;

    IF ISNULL(@StockItemMasterId, 0) = 0
    BEGIN
        INSERT INTO dbo.StockItemMasters
        (
            InventoryUnitId, InventoryCategoryId, ItemType, ItemName, Specification,
            MinimumStock, MaximumStock, IsActive, CreatedAt
        )
        VALUES
        (
            @InventoryUnitId, @InventoryCategoryId, @ItemType, @ItemName, NULLIF(LTRIM(RTRIM(@Specification)), N''),
            @MinimumStock, @MaximumStock, @IsActive, SYSUTCDATETIME()
        );
        SET @StockItemMasterId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.StockItemMasters
        SET InventoryUnitId = @InventoryUnitId,
            InventoryCategoryId = @InventoryCategoryId,
            ItemType = @ItemType,
            ItemName = @ItemName,
            Specification = NULLIF(LTRIM(RTRIM(@Specification)), N''),
            MinimumStock = @MinimumStock,
            MaximumStock = @MaximumStock,
            IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        WHERE StockItemMasterId = @StockItemMasterId;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SaveSupplier
    @SupplierId BIGINT OUTPUT,
    @SupplierName NVARCHAR(150),
    @SupplierCode NVARCHAR(40),
    @ContactPerson NVARCHAR(120) = NULL,
    @ContactPhone NVARCHAR(30) = NULL,
    @ContactEmail NVARCHAR(150) = NULL,
    @Address NVARCHAR(300) = NULL,
    @PaymentTermsDays INT,
    @Notes NVARCHAR(400) = NULL,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    SET @SupplierName = LTRIM(RTRIM(@SupplierName));
    SET @SupplierCode = UPPER(LTRIM(RTRIM(@SupplierCode)));
    IF @SupplierName = N'' OR @SupplierCode = N'' THROW 50001, N'Supplier name and code are required.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Suppliers WHERE (SupplierName = @SupplierName OR SupplierCode = @SupplierCode) AND SupplierId <> ISNULL(@SupplierId, 0))
        THROW 50001, N'Supplier name or code already exists.', 1;

    IF ISNULL(@SupplierId, 0) = 0
    BEGIN
        INSERT INTO dbo.Suppliers
        (
            SupplierName, SupplierCode, ContactPerson, ContactPhone, ContactEmail, Address,
            PaymentTermsDays, Notes, IsActive, CreatedAt
        )
        VALUES
        (
            @SupplierName, @SupplierCode, NULLIF(LTRIM(RTRIM(@ContactPerson)), N''), NULLIF(LTRIM(RTRIM(@ContactPhone)), N''), NULLIF(LTRIM(RTRIM(@ContactEmail)), N''),
            NULLIF(LTRIM(RTRIM(@Address)), N''), @PaymentTermsDays, NULLIF(LTRIM(RTRIM(@Notes)), N''), @IsActive, SYSUTCDATETIME()
        );
        SET @SupplierId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.Suppliers
        SET SupplierName = @SupplierName,
            SupplierCode = @SupplierCode,
            ContactPerson = NULLIF(LTRIM(RTRIM(@ContactPerson)), N''),
            ContactPhone = NULLIF(LTRIM(RTRIM(@ContactPhone)), N''),
            ContactEmail = NULLIF(LTRIM(RTRIM(@ContactEmail)), N''),
            Address = NULLIF(LTRIM(RTRIM(@Address)), N''),
            PaymentTermsDays = @PaymentTermsDays,
            Notes = NULLIF(LTRIM(RTRIM(@Notes)), N''),
            IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        WHERE SupplierId = @SupplierId;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SaveLocation
    @StockLocationId BIGINT OUTPUT,
    @BranchId BIGINT = NULL,
    @Name NVARCHAR(120),
    @Code NVARCHAR(40),
    @LocationType NVARCHAR(30),
    @Description NVARCHAR(250) = NULL,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Name = LTRIM(RTRIM(@Name));
    SET @Code = UPPER(LTRIM(RTRIM(@Code)));
    SET @LocationType = LTRIM(RTRIM(@LocationType));

    IF @Name = N'' OR @Code = N'' THROW 50001, N'Location name and code are required.', 1;
    IF @LocationType NOT IN (N'MainStore', N'PharmacyStore', N'LabStore', N'OtStore', N'WardStock')
        THROW 50001, N'Invalid location type.', 1;
    IF @BranchId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE BranchId = @BranchId)
        THROW 50001, N'Branch not found.', 1;
    IF EXISTS (SELECT 1 FROM dbo.StockLocations WHERE (Name = @Name OR Code = @Code) AND StockLocationId <> ISNULL(@StockLocationId, 0))
        THROW 50001, N'Stock location name or code already exists.', 1;

    IF ISNULL(@StockLocationId, 0) = 0
    BEGIN
        INSERT INTO dbo.StockLocations
        (
            BranchId, Name, Code, LocationType, Description, IsActive, CreatedAt
        )
        VALUES
        (
            @BranchId, @Name, @Code, @LocationType, NULLIF(LTRIM(RTRIM(@Description)), N''), @IsActive, SYSUTCDATETIME()
        );
        SET @StockLocationId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.StockLocations
        SET BranchId = @BranchId,
            Name = @Name,
            Code = @Code,
            LocationType = @LocationType,
            Description = NULLIF(LTRIM(RTRIM(@Description)), N''),
            IsActive = @IsActive,
            UpdatedAt = SYSUTCDATETIME()
        WHERE StockLocationId = @StockLocationId;
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SavePurchaseOrder
    @PurchaseOrderId BIGINT OUTPUT,
    @SupplierId BIGINT,
    @StockLocationId BIGINT,
    @OrderDate DATE,
    @ExpectedDeliveryDate DATE = NULL,
    @Status NVARCHAR(30),
    @Notes NVARCHAR(400) = NULL,
    @Lines dbo.InventoryLineInputType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE SupplierId = @SupplierId) THROW 50001, N'Supplier not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.StockLocations WHERE StockLocationId = @StockLocationId) THROW 50001, N'Stock location not found.', 1;
    IF @Status NOT IN (N'Draft', N'Ordered', N'Cancelled') SET @Status = N'Ordered';
    IF NOT EXISTS (SELECT 1 FROM @Lines WHERE Quantity > 0) THROW 50001, N'At least one purchase line is required.', 1;

    IF ISNULL(@PurchaseOrderId, 0) <> 0
       AND EXISTS (SELECT 1 FROM dbo.PurchaseOrderLines WHERE PurchaseOrderId = @PurchaseOrderId AND ReceivedQuantity > 0)
        THROW 50001, N'Purchase order cannot be edited after stock receipt has started.', 1;

    IF ISNULL(@PurchaseOrderId, 0) = 0
    BEGIN
        DECLARE @NextPoNo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT, RIGHT(OrderNumber, 6))) FROM dbo.PurchaseOrders), 0) + 1;
        DECLARE @OrderNumber NVARCHAR(30) = CONCAT(N'PO-', RIGHT(CONCAT(N'000000', @NextPoNo), 6));

        INSERT INTO dbo.PurchaseOrders
        (
            OrderNumber, SupplierId, StockLocationId, OrderDate, ExpectedDeliveryDate, Status, Notes, CreatedAt
        )
        VALUES
        (
            @OrderNumber, @SupplierId, @StockLocationId, @OrderDate, @ExpectedDeliveryDate, @Status, NULLIF(LTRIM(RTRIM(@Notes)), N''), SYSUTCDATETIME()
        );
        SET @PurchaseOrderId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE dbo.PurchaseOrders
        SET SupplierId = @SupplierId,
            StockLocationId = @StockLocationId,
            OrderDate = @OrderDate,
            ExpectedDeliveryDate = @ExpectedDeliveryDate,
            Status = @Status,
            Notes = NULLIF(LTRIM(RTRIM(@Notes)), N''),
            UpdatedAt = SYSUTCDATETIME()
        WHERE PurchaseOrderId = @PurchaseOrderId;

        DELETE FROM dbo.PurchaseOrderLines WHERE PurchaseOrderId = @PurchaseOrderId;
    END

    INSERT INTO dbo.PurchaseOrderLines
    (
        PurchaseOrderId, MedicineMasterId, StockItemMasterId, ItemName, UnitName,
        OrderedQuantity, ReceivedQuantity, UnitCost, Notes, CreatedAt
    )
    SELECT
        @PurchaseOrderId,
        l.MedicineMasterId,
        l.StockItemMasterId,
        COALESCE(NULLIF(LTRIM(RTRIM(l.ItemName)), N''), m.MedicineName, i.ItemName),
        COALESCE(NULLIF(LTRIM(RTRIM(l.UnitName)), N''), u.Name),
        l.Quantity,
        0,
        l.UnitCost,
        NULLIF(LTRIM(RTRIM(l.Notes)), N''),
        SYSUTCDATETIME()
    FROM @Lines l
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = l.MedicineMasterId
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = l.StockItemMasterId
    LEFT JOIN dbo.InventoryUnits u ON u.InventoryUnitId = COALESCE(m.InventoryUnitId, i.InventoryUnitId)
    WHERE l.Quantity > 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_ReceivePurchaseOrder
    @PurchaseInvoiceId BIGINT OUTPUT,
    @PurchaseOrderId BIGINT,
    @InvoiceNumber NVARCHAR(50),
    @InvoiceDate DATE,
    @DueDate DATE = NULL,
    @PaidAmount DECIMAL(18, 2),
    @Notes NVARCHAR(400) = NULL,
    @Lines dbo.InventoryLineInputType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SupplierId BIGINT;
    DECLARE @StockLocationId BIGINT;

    SELECT @SupplierId = SupplierId, @StockLocationId = StockLocationId
    FROM dbo.PurchaseOrders
    WHERE PurchaseOrderId = @PurchaseOrderId;

    IF @SupplierId IS NULL THROW 50001, N'Purchase order not found.', 1;
    IF EXISTS (SELECT 1 FROM dbo.PurchaseInvoices WHERE SupplierId = @SupplierId AND InvoiceNumber = LTRIM(RTRIM(@InvoiceNumber)))
        THROW 50001, N'Purchase invoice number already exists for this supplier.', 1;

    SELECT
        COALESCE(pol.PurchaseOrderLineId, l.ReferenceLineId) AS PurchaseOrderLineId,
        COALESCE(l.MedicineMasterId, pol.MedicineMasterId) AS MedicineMasterId,
        COALESCE(l.StockItemMasterId, pol.StockItemMasterId) AS StockItemMasterId,
        COALESCE(NULLIF(LTRIM(RTRIM(l.ItemName)), N''), pol.ItemName, m.MedicineName, i.ItemName) AS ItemName,
        COALESCE(NULLIF(LTRIM(RTRIM(l.UnitName)), N''), pol.UnitName, u.Name) AS UnitName,
        NULLIF(LTRIM(RTRIM(l.BatchNumber)), N'') AS BatchNumber,
        l.ExpiryDate,
        l.Quantity,
        CASE WHEN l.UnitCost > 0 THEN l.UnitCost ELSE ISNULL(pol.UnitCost, 0) END AS UnitCost,
        NULLIF(LTRIM(RTRIM(l.Notes)), N'') AS Notes,
        ISNULL(m.BatchRequired, 0) AS BatchRequired
    INTO #ReceiveLines
    FROM @Lines l
    LEFT JOIN dbo.PurchaseOrderLines pol ON pol.PurchaseOrderLineId = l.ReferenceLineId AND pol.PurchaseOrderId = @PurchaseOrderId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = COALESCE(l.MedicineMasterId, pol.MedicineMasterId)
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = COALESCE(l.StockItemMasterId, pol.StockItemMasterId)
    LEFT JOIN dbo.InventoryUnits u ON u.InventoryUnitId = COALESCE(m.InventoryUnitId, i.InventoryUnitId)
    WHERE l.Quantity > 0;

    IF NOT EXISTS (SELECT 1 FROM #ReceiveLines) THROW 50001, N'At least one received line is required.', 1;
    IF EXISTS (SELECT 1 FROM #ReceiveLines WHERE BatchRequired = 1 AND BatchNumber IS NULL)
        THROW 50001, N'Batch number is required for one or more medicine lines.', 1;

    INSERT INTO dbo.PurchaseInvoices
    (
        PurchaseOrderId, SupplierId, StockLocationId, InvoiceNumber, InvoiceDate, DueDate,
        TotalAmount, PaidAmount, Status, Notes, CreatedAt
    )
    VALUES
    (
        @PurchaseOrderId, @SupplierId, @StockLocationId, LTRIM(RTRIM(@InvoiceNumber)), @InvoiceDate, @DueDate,
        0, @PaidAmount, N'Open', NULLIF(LTRIM(RTRIM(@Notes)), N''), SYSUTCDATETIME()
    );
    SET @PurchaseInvoiceId = SCOPE_IDENTITY();

    INSERT INTO dbo.PurchaseInvoiceLines
    (
        PurchaseInvoiceId, PurchaseOrderLineId, MedicineMasterId, StockItemMasterId, ItemName, UnitName,
        BatchNumber, ExpiryDate, Quantity, UnitCost, LineTotal, Notes, CreatedAt
    )
    SELECT
        @PurchaseInvoiceId, PurchaseOrderLineId, MedicineMasterId, StockItemMasterId, ItemName, UnitName,
        BatchNumber, ExpiryDate, Quantity, UnitCost, Quantity * UnitCost, Notes, SYSUTCDATETIME()
    FROM #ReceiveLines;

    UPDATE pol
    SET pol.ReceivedQuantity = pol.ReceivedQuantity + rl.Quantity,
        pol.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.PurchaseOrderLines pol
    INNER JOIN #ReceiveLines rl ON rl.PurchaseOrderLineId = pol.PurchaseOrderLineId;

    UPDATE sb
    SET sb.QuantityOnHand = sb.QuantityOnHand + rl.Quantity,
        sb.UnitCost = CASE WHEN rl.UnitCost > 0 THEN rl.UnitCost ELSE sb.UnitCost END,
        sb.LastMovementAt = SYSUTCDATETIME(),
        sb.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.StockBatches sb
    INNER JOIN #ReceiveLines rl
        ON sb.StockLocationId = @StockLocationId
       AND ISNULL(sb.MedicineMasterId, 0) = ISNULL(rl.MedicineMasterId, 0)
       AND ISNULL(sb.StockItemMasterId, 0) = ISNULL(rl.StockItemMasterId, 0)
       AND ISNULL(sb.BatchNumber, N'') = ISNULL(rl.BatchNumber, N'')
       AND ISNULL(sb.ExpiryDate, '19000101') = ISNULL(rl.ExpiryDate, '19000101');

    INSERT INTO dbo.StockBatches
    (
        StockLocationId, MedicineMasterId, StockItemMasterId, BatchNumber, ExpiryDate,
        QuantityOnHand, UnitCost, LastMovementAt, CreatedAt
    )
    SELECT
        @StockLocationId, rl.MedicineMasterId, rl.StockItemMasterId, rl.BatchNumber, rl.ExpiryDate,
        rl.Quantity, rl.UnitCost, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM #ReceiveLines rl
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.StockBatches sb
        WHERE sb.StockLocationId = @StockLocationId
          AND ISNULL(sb.MedicineMasterId, 0) = ISNULL(rl.MedicineMasterId, 0)
          AND ISNULL(sb.StockItemMasterId, 0) = ISNULL(rl.StockItemMasterId, 0)
          AND ISNULL(sb.BatchNumber, N'') = ISNULL(rl.BatchNumber, N'')
          AND ISNULL(sb.ExpiryDate, '19000101') = ISNULL(rl.ExpiryDate, '19000101')
    );

    UPDATE dbo.PurchaseInvoices
    SET TotalAmount = (SELECT ISNULL(SUM(LineTotal), 0) FROM dbo.PurchaseInvoiceLines WHERE PurchaseInvoiceId = @PurchaseInvoiceId),
        Status = CASE
                    WHEN @PaidAmount <= 0 THEN N'Open'
                    WHEN @PaidAmount >= (SELECT ISNULL(SUM(LineTotal), 0) FROM dbo.PurchaseInvoiceLines WHERE PurchaseInvoiceId = @PurchaseInvoiceId) THEN N'Paid'
                    ELSE N'Partial'
                 END,
        UpdatedAt = SYSUTCDATETIME()
    WHERE PurchaseInvoiceId = @PurchaseInvoiceId;

    UPDATE dbo.PurchaseOrders
    SET Status = CASE
                    WHEN NOT EXISTS (SELECT 1 FROM dbo.PurchaseOrderLines WHERE PurchaseOrderId = @PurchaseOrderId AND OrderedQuantity > ReceivedQuantity) THEN N'Received'
                    ELSE N'PartiallyReceived'
                 END,
        UpdatedAt = SYSUTCDATETIME()
    WHERE PurchaseOrderId = @PurchaseOrderId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SavePurchaseReturn
    @PurchaseReturnId BIGINT OUTPUT,
    @PurchaseInvoiceId BIGINT,
    @ReturnDate DATE,
    @Notes NVARCHAR(400) = NULL,
    @Lines dbo.InventoryLineInputType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SupplierId BIGINT;
    DECLARE @StockLocationId BIGINT;

    SELECT @SupplierId = SupplierId, @StockLocationId = StockLocationId
    FROM dbo.PurchaseInvoices
    WHERE PurchaseInvoiceId = @PurchaseInvoiceId;

    IF @SupplierId IS NULL THROW 50001, N'Purchase invoice not found.', 1;

    SELECT
        COALESCE(pil.PurchaseInvoiceLineId, l.ReferenceLineId) AS PurchaseInvoiceLineId,
        COALESCE(l.MedicineMasterId, pil.MedicineMasterId) AS MedicineMasterId,
        COALESCE(l.StockItemMasterId, pil.StockItemMasterId) AS StockItemMasterId,
        pil.ItemName,
        COALESCE(NULLIF(LTRIM(RTRIM(l.BatchNumber)), N''), pil.BatchNumber) AS BatchNumber,
        COALESCE(l.ExpiryDate, pil.ExpiryDate) AS ExpiryDate,
        l.Quantity,
        CASE WHEN l.UnitCost > 0 THEN l.UnitCost ELSE pil.UnitCost END AS UnitCost,
        NULLIF(LTRIM(RTRIM(l.Reason)), N'') AS Reason
    INTO #ReturnLines
    FROM @Lines l
    LEFT JOIN dbo.PurchaseInvoiceLines pil ON pil.PurchaseInvoiceLineId = l.ReferenceLineId AND pil.PurchaseInvoiceId = @PurchaseInvoiceId
    WHERE l.Quantity > 0;

    IF NOT EXISTS (SELECT 1 FROM #ReturnLines) THROW 50001, N'At least one return line is required.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM #ReturnLines rl
        OUTER APPLY
        (
            SELECT TOP 1 sb.QuantityOnHand
            FROM dbo.StockBatches sb
            WHERE sb.StockLocationId = @StockLocationId
              AND ISNULL(sb.MedicineMasterId, 0) = ISNULL(rl.MedicineMasterId, 0)
              AND ISNULL(sb.StockItemMasterId, 0) = ISNULL(rl.StockItemMasterId, 0)
              AND ISNULL(sb.BatchNumber, N'') = ISNULL(rl.BatchNumber, N'')
              AND ISNULL(sb.ExpiryDate, '19000101') = ISNULL(rl.ExpiryDate, '19000101')
        ) stock
        WHERE ISNULL(stock.QuantityOnHand, 0) < rl.Quantity
    )
        THROW 50001, N'Insufficient stock available for one or more return lines.', 1;

    DECLARE @NextReturnNo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT, RIGHT(ReturnNumber, 6))) FROM dbo.PurchaseReturns), 0) + 1;
    DECLARE @ReturnNumber NVARCHAR(30) = CONCAT(N'PR-', RIGHT(CONCAT(N'000000', @NextReturnNo), 6));

    INSERT INTO dbo.PurchaseReturns
    (
        PurchaseInvoiceId, SupplierId, ReturnNumber, ReturnDate, TotalAmount, Notes, CreatedAt
    )
    VALUES
    (
        @PurchaseInvoiceId, @SupplierId, @ReturnNumber, @ReturnDate, 0, NULLIF(LTRIM(RTRIM(@Notes)), N''), SYSUTCDATETIME()
    );
    SET @PurchaseReturnId = SCOPE_IDENTITY();

    INSERT INTO dbo.PurchaseReturnLines
    (
        PurchaseReturnId, PurchaseInvoiceLineId, MedicineMasterId, StockItemMasterId, ItemName,
        BatchNumber, ExpiryDate, Quantity, UnitCost, LineTotal, Reason, CreatedAt
    )
    SELECT
        @PurchaseReturnId, PurchaseInvoiceLineId, MedicineMasterId, StockItemMasterId, ItemName,
        BatchNumber, ExpiryDate, Quantity, UnitCost, Quantity * UnitCost, Reason, SYSUTCDATETIME()
    FROM #ReturnLines;

    UPDATE sb
    SET sb.QuantityOnHand = sb.QuantityOnHand - rl.Quantity,
        sb.LastMovementAt = SYSUTCDATETIME(),
        sb.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.StockBatches sb
    INNER JOIN #ReturnLines rl
        ON sb.StockLocationId = @StockLocationId
       AND ISNULL(sb.MedicineMasterId, 0) = ISNULL(rl.MedicineMasterId, 0)
       AND ISNULL(sb.StockItemMasterId, 0) = ISNULL(rl.StockItemMasterId, 0)
       AND ISNULL(sb.BatchNumber, N'') = ISNULL(rl.BatchNumber, N'')
       AND ISNULL(sb.ExpiryDate, '19000101') = ISNULL(rl.ExpiryDate, '19000101');

    UPDATE dbo.PurchaseReturns
    SET TotalAmount = (SELECT ISNULL(SUM(LineTotal), 0) FROM dbo.PurchaseReturnLines WHERE PurchaseReturnId = @PurchaseReturnId)
    WHERE PurchaseReturnId = @PurchaseReturnId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SaveTransfer
    @StockTransferId BIGINT OUTPUT,
    @FromStockLocationId BIGINT,
    @ToStockLocationId BIGINT,
    @TransferDate DATE,
    @Notes NVARCHAR(400) = NULL,
    @Lines dbo.InventoryLineInputType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    IF @FromStockLocationId = @ToStockLocationId THROW 50001, N'Source and destination locations must be different.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.StockLocations WHERE StockLocationId = @FromStockLocationId) THROW 50001, N'Source location not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.StockLocations WHERE StockLocationId = @ToStockLocationId) THROW 50001, N'Destination location not found.', 1;
    IF NOT EXISTS (SELECT 1 FROM @Lines WHERE Quantity > 0 AND StockBatchId IS NOT NULL) THROW 50001, N'At least one transfer line is required.', 1;

    IF ISNULL(@StockTransferId, 0) = 0
    BEGIN
        DECLARE @NextTransferNo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT, RIGHT(TransferNumber, 6))) FROM dbo.StockTransfers), 0) + 1;
        DECLARE @TransferNumber NVARCHAR(30) = CONCAT(N'TR-', RIGHT(CONCAT(N'000000', @NextTransferNo), 6));

        INSERT INTO dbo.StockTransfers
        (
            TransferNumber, FromStockLocationId, ToStockLocationId, TransferDate, Status, Notes, CreatedAt
        )
        VALUES
        (
            @TransferNumber, @FromStockLocationId, @ToStockLocationId, @TransferDate, N'Pending', NULLIF(LTRIM(RTRIM(@Notes)), N''), SYSUTCDATETIME()
        );
        SET @StockTransferId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.StockTransfers WHERE StockTransferId = @StockTransferId AND Status = N'Received')
            THROW 50001, N'Received transfer cannot be edited.', 1;

        UPDATE dbo.StockTransfers
        SET FromStockLocationId = @FromStockLocationId,
            ToStockLocationId = @ToStockLocationId,
            TransferDate = @TransferDate,
            Notes = NULLIF(LTRIM(RTRIM(@Notes)), N''),
            Status = N'Pending',
            UpdatedAt = SYSUTCDATETIME()
        WHERE StockTransferId = @StockTransferId;

        DELETE FROM dbo.StockTransferLines WHERE StockTransferId = @StockTransferId;
    END

    IF EXISTS
    (
        SELECT 1
        FROM @Lines l
        INNER JOIN dbo.StockBatches sb ON sb.StockBatchId = l.StockBatchId
        WHERE l.Quantity > sb.QuantityOnHand
           OR sb.StockLocationId <> @FromStockLocationId
           OR (sb.ExpiryDate IS NOT NULL AND sb.ExpiryDate < CAST(GETDATE() AS DATE))
    )
        THROW 50001, N'One or more transfer lines are invalid, expired, or exceed available stock.', 1;

    INSERT INTO dbo.StockTransferLines
    (
        StockTransferId, StockBatchId, ItemName, BatchNumber, ExpiryDate, Quantity, CreatedAt
    )
    SELECT
        @StockTransferId,
        sb.StockBatchId,
        COALESCE(m.MedicineName, i.ItemName),
        sb.BatchNumber,
        sb.ExpiryDate,
        l.Quantity,
        SYSUTCDATETIME()
    FROM @Lines l
    INNER JOIN dbo.StockBatches sb ON sb.StockBatchId = l.StockBatchId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = sb.MedicineMasterId
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = sb.StockItemMasterId
    WHERE l.Quantity > 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_ApproveTransfer
    @StockTransferId BIGINT,
    @Notes NVARCHAR(400) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.StockTransfers WHERE StockTransferId = @StockTransferId) THROW 50001, N'Stock transfer not found.', 1;

    UPDATE dbo.StockTransfers
    SET Status = N'Approved',
        Notes = COALESCE(NULLIF(LTRIM(RTRIM(@Notes)), N''), Notes),
        ApprovedAt = SYSUTCDATETIME(),
        UpdatedAt = SYSUTCDATETIME()
    WHERE StockTransferId = @StockTransferId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_ReceiveTransfer
    @StockTransferId BIGINT,
    @Notes NVARCHAR(400) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromStockLocationId BIGINT;
    DECLARE @ToStockLocationId BIGINT;

    SELECT @FromStockLocationId = FromStockLocationId, @ToStockLocationId = ToStockLocationId
    FROM dbo.StockTransfers
    WHERE StockTransferId = @StockTransferId;

    IF @FromStockLocationId IS NULL THROW 50001, N'Stock transfer not found.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.StockTransferLines stl
        INNER JOIN dbo.StockBatches sb ON sb.StockBatchId = stl.StockBatchId
        WHERE stl.StockTransferId = @StockTransferId
          AND (sb.QuantityOnHand < stl.Quantity OR (sb.ExpiryDate IS NOT NULL AND sb.ExpiryDate < CAST(GETDATE() AS DATE)))
    )
        THROW 50001, N'One or more transfer batches are expired or do not have enough quantity.', 1;

    UPDATE sb
    SET sb.QuantityOnHand = sb.QuantityOnHand - stl.Quantity,
        sb.LastMovementAt = SYSUTCDATETIME(),
        sb.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.StockBatches sb
    INNER JOIN dbo.StockTransferLines stl ON stl.StockBatchId = sb.StockBatchId
    WHERE stl.StockTransferId = @StockTransferId;

    UPDATE dest
    SET dest.QuantityOnHand = dest.QuantityOnHand + stl.Quantity,
        dest.UnitCost = src.UnitCost,
        dest.LastMovementAt = SYSUTCDATETIME(),
        dest.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.StockBatches dest
    INNER JOIN dbo.StockTransferLines stl ON stl.StockTransferId = @StockTransferId
    INNER JOIN dbo.StockBatches src ON src.StockBatchId = stl.StockBatchId
        AND dest.StockLocationId = @ToStockLocationId
        AND ISNULL(dest.MedicineMasterId, 0) = ISNULL(src.MedicineMasterId, 0)
        AND ISNULL(dest.StockItemMasterId, 0) = ISNULL(src.StockItemMasterId, 0)
        AND ISNULL(dest.BatchNumber, N'') = ISNULL(src.BatchNumber, N'')
        AND ISNULL(dest.ExpiryDate, '19000101') = ISNULL(src.ExpiryDate, '19000101');

    INSERT INTO dbo.StockBatches
    (
        StockLocationId, MedicineMasterId, StockItemMasterId, BatchNumber, ExpiryDate,
        QuantityOnHand, UnitCost, LastMovementAt, CreatedAt
    )
    SELECT
        @ToStockLocationId, src.MedicineMasterId, src.StockItemMasterId, src.BatchNumber, src.ExpiryDate,
        stl.Quantity, src.UnitCost, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM dbo.StockTransferLines stl
    INNER JOIN dbo.StockBatches src ON src.StockBatchId = stl.StockBatchId
    WHERE stl.StockTransferId = @StockTransferId
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.StockBatches dest
          WHERE dest.StockLocationId = @ToStockLocationId
            AND ISNULL(dest.MedicineMasterId, 0) = ISNULL(src.MedicineMasterId, 0)
            AND ISNULL(dest.StockItemMasterId, 0) = ISNULL(src.StockItemMasterId, 0)
            AND ISNULL(dest.BatchNumber, N'') = ISNULL(src.BatchNumber, N'')
            AND ISNULL(dest.ExpiryDate, '19000101') = ISNULL(src.ExpiryDate, '19000101')
      );

    UPDATE dbo.StockTransfers
    SET Status = N'Received',
        Notes = COALESCE(NULLIF(LTRIM(RTRIM(@Notes)), N''), Notes),
        ApprovedAt = COALESCE(ApprovedAt, SYSUTCDATETIME()),
        ReceivedAt = SYSUTCDATETIME(),
        UpdatedAt = SYSUTCDATETIME()
    WHERE StockTransferId = @StockTransferId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_SaveAdjustment
    @StockAdjustmentId BIGINT OUTPUT,
    @StockLocationId BIGINT,
    @Reason NVARCHAR(30),
    @AdjustmentDate DATE,
    @Notes NVARCHAR(400) = NULL,
    @Lines dbo.InventoryLineInputType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.StockLocations WHERE StockLocationId = @StockLocationId) THROW 50001, N'Stock location not found.', 1;
    IF @Reason NOT IN (N'Damage', N'Loss', N'ExpiryRemoval', N'ManualCorrection') THROW 50001, N'Invalid adjustment reason.', 1;
    IF NOT EXISTS (SELECT 1 FROM @Lines WHERE Quantity <> 0 AND StockBatchId IS NOT NULL) THROW 50001, N'At least one adjustment line is required.', 1;

    IF ISNULL(@StockAdjustmentId, 0) = 0
    BEGIN
        DECLARE @NextAdjustmentNo INT = ISNULL((SELECT MAX(TRY_CONVERT(INT, RIGHT(AdjustmentNumber, 6))) FROM dbo.StockAdjustments), 0) + 1;
        DECLARE @AdjustmentNumber NVARCHAR(30) = CONCAT(N'ADJ-', RIGHT(CONCAT(N'000000', @NextAdjustmentNo), 6));

        INSERT INTO dbo.StockAdjustments
        (
            AdjustmentNumber, StockLocationId, Reason, Status, AdjustmentDate, Notes, CreatedAt
        )
        VALUES
        (
            @AdjustmentNumber, @StockLocationId, @Reason, N'Pending', @AdjustmentDate, NULLIF(LTRIM(RTRIM(@Notes)), N''), SYSUTCDATETIME()
        );
        SET @StockAdjustmentId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.StockAdjustments WHERE StockAdjustmentId = @StockAdjustmentId AND Status = N'Approved')
            THROW 50001, N'Approved adjustment cannot be edited.', 1;

        UPDATE dbo.StockAdjustments
        SET StockLocationId = @StockLocationId,
            Reason = @Reason,
            AdjustmentDate = @AdjustmentDate,
            Notes = NULLIF(LTRIM(RTRIM(@Notes)), N''),
            Status = N'Pending',
            UpdatedAt = SYSUTCDATETIME()
        WHERE StockAdjustmentId = @StockAdjustmentId;

        DELETE FROM dbo.StockAdjustmentLines WHERE StockAdjustmentId = @StockAdjustmentId;
    END

    INSERT INTO dbo.StockAdjustmentLines
    (
        StockAdjustmentId, StockBatchId, ItemName, BatchNumber, ExpiryDate, QuantityDelta, Notes, CreatedAt
    )
    SELECT
        @StockAdjustmentId,
        sb.StockBatchId,
        COALESCE(m.MedicineName, i.ItemName),
        sb.BatchNumber,
        sb.ExpiryDate,
        l.Quantity,
        NULLIF(LTRIM(RTRIM(l.Notes)), N''),
        SYSUTCDATETIME()
    FROM @Lines l
    INNER JOIN dbo.StockBatches sb ON sb.StockBatchId = l.StockBatchId
    LEFT JOIN dbo.MedicineMasters m ON m.MedicineMasterId = sb.MedicineMasterId
    LEFT JOIN dbo.StockItemMasters i ON i.StockItemMasterId = sb.StockItemMasterId
    WHERE l.Quantity <> 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_Inventory_ApproveAdjustment
    @StockAdjustmentId BIGINT,
    @Notes NVARCHAR(400) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.StockAdjustments WHERE StockAdjustmentId = @StockAdjustmentId) THROW 50001, N'Stock adjustment not found.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.StockAdjustmentLines sal
        INNER JOIN dbo.StockBatches sb ON sb.StockBatchId = sal.StockBatchId
        WHERE sal.StockAdjustmentId = @StockAdjustmentId
          AND sb.QuantityOnHand + sal.QuantityDelta < 0
    )
        THROW 50001, N'One or more adjustment lines would result in negative stock.', 1;

    UPDATE sb
    SET sb.QuantityOnHand = sb.QuantityOnHand + sal.QuantityDelta,
        sb.LastMovementAt = SYSUTCDATETIME(),
        sb.UpdatedAt = SYSUTCDATETIME()
    FROM dbo.StockBatches sb
    INNER JOIN dbo.StockAdjustmentLines sal ON sal.StockBatchId = sb.StockBatchId
    WHERE sal.StockAdjustmentId = @StockAdjustmentId;

    UPDATE dbo.StockAdjustments
    SET Status = N'Approved',
        Notes = COALESCE(NULLIF(LTRIM(RTRIM(@Notes)), N''), Notes),
        ApprovedAt = SYSUTCDATETIME(),
        UpdatedAt = SYSUTCDATETIME()
    WHERE StockAdjustmentId = @StockAdjustmentId;
END;
GO
