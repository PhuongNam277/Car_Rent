-- 1) Tạo bảng Locations (station/chi nhánh)
IF OBJECT_ID('dbo.Locations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Locations (
        LocationId      INT IDENTITY(1,1) PRIMARY KEY,
        Name            NVARCHAR(100) NOT NULL,
        Address         NVARCHAR(255) NULL,
        City            NVARCHAR(100) NULL,
        Lat             DECIMAL(9,6)  NULL,
        Lng             DECIMAL(9,6)  NULL,
        TimeZone        NVARCHAR(50)  NULL,
        IsActive        BIT           NOT NULL DEFAULT(1),
        CreatedAt       DATETIME      NOT NULL DEFAULT GETDATE()
    );
END;

-- 2) Seed vài chi nhánh mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.Locations WHERE Name = N'Center Depot')
    INSERT dbo.Locations (Name, Address, City) VALUES (N'Center Depot', N'12 Main St', N'Ho Chi Minh');
IF NOT EXISTS (SELECT 1 FROM dbo.Locations WHERE Name = N'Airport Depot')
    INSERT dbo.Locations (Name, Address, City) VALUES (N'Airport Depot', N'Tan Son Nhat', N'Ho Chi Minh');



	-- 3) Thêm cột BaseLocationId cho Cars (tạm cho phép NULL để update dần)
IF COL_LENGTH('dbo.Cars', 'BaseLocationId') IS NULL
    ALTER TABLE dbo.Cars ADD BaseLocationId INT NULL;

-- 4) Set toàn bộ xe về 'Center Depot' (hoặc station anh muốn)
DECLARE @CenterDepotId INT = (SELECT TOP 1 LocationId FROM dbo.Locations WHERE Name = N'Center Depot' ORDER BY LocationId);
UPDATE dbo.Cars SET BaseLocationId = @CenterDepotId WHERE BaseLocationId IS NULL;

-- 5) Tạo FK + Index rồi siết NOT NULL
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Cars_Locations_BaseLocationId')
BEGIN
    ALTER TABLE dbo.Cars WITH CHECK 
    ADD CONSTRAINT FK_Cars_Locations_BaseLocationId FOREIGN KEY (BaseLocationId) REFERENCES dbo.Locations(LocationId);
END;

-- Index giúp lọc theo Location nhanh
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Cars_BaseLocationId')
    CREATE INDEX IX_Cars_BaseLocationId ON dbo.Cars(BaseLocationId);

-- 6) Ép NOT NULL sau khi đã fill dữ liệu
ALTER TABLE dbo.Cars ALTER COLUMN BaseLocationId INT NOT NULL;











-- 7) Thêm cột PickupLocationId, DropoffLocationId
IF COL_LENGTH('dbo.Reservations', 'PickupLocationId') IS NULL
    ALTER TABLE dbo.Reservations ADD PickupLocationId INT NULL;
IF COL_LENGTH('dbo.Reservations', 'DropoffLocationId') IS NULL
    ALTER TABLE dbo.Reservations ADD DropoffLocationId INT NULL;

-- 8) Map dữ liệu cũ: tạm set = BaseLocation của xe (one-way chưa bật)
UPDATE r
SET r.PickupLocationId = c.BaseLocationId,
    r.DropoffLocationId = c.BaseLocationId
FROM dbo.Reservations r
JOIN dbo.Cars c ON c.CarId = r.CarId
WHERE r.PickupLocationId IS NULL OR r.DropoffLocationId IS NULL;

-- 9) Tạo FK + Index
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reservations_Locations_Pickup')
BEGIN
    ALTER TABLE dbo.Reservations WITH CHECK 
    ADD CONSTRAINT FK_Reservations_Locations_Pickup FOREIGN KEY (PickupLocationId) REFERENCES dbo.Locations(LocationId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reservations_Locations_Dropoff')
BEGIN
    ALTER TABLE dbo.Reservations WITH CHECK 
    ADD CONSTRAINT FK_Reservations_Locations_Dropoff FOREIGN KEY (DropoffLocationId) REFERENCES dbo.Locations(LocationId);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_Pickup')
    CREATE INDEX IX_Reservations_Pickup ON dbo.Reservations(PickupLocationId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_Dropoff')
    CREATE INDEX IX_Reservations_Dropoff ON dbo.Reservations(DropoffLocationId);

-- 10) (Tuỳ anh) dần dần NGỪNG dùng FromCity/ToCity ở code; giữ cột cũ để tương thích UI hiện tại

