-- Script tạo cơ sở dữ liệu cho website cho thuê ô tô
CREATE DATABASE CarRentalDB;
GO

USE CarRentalDB;
GO

-- Bảng Users
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PhoneNumber NVARCHAR(15),
    Role NVARCHAR(20) CHECK (Role IN ('Customer', 'Admin', 'Staff')),
    CreatedDate DATETIME DEFAULT GETDATE()
);

-- Bảng Categories
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(50) NOT NULL UNIQUE
);

-- Bảng Cars
CREATE TABLE Cars (
    CarId INT IDENTITY(1,1) PRIMARY KEY,
    CarName NVARCHAR(100) NOT NULL,
    Brand NVARCHAR(50) NOT NULL,
    Model NVARCHAR(50) NOT NULL,
    LicensePlate NVARCHAR(20) NOT NULL UNIQUE,
    CategoryId INT NOT NULL FOREIGN KEY REFERENCES Categories(CategoryId),
    ImageUrl NVARCHAR(255),
    RentalPricePerDay DECIMAL(10, 2) NOT NULL,
    Status NVARCHAR(20) CHECK (Status IN ('Available', 'Rented', 'Maintenance')) DEFAULT 'Available'
);

-- Bảng Reservations
CREATE TABLE Reservations (
    ReservationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    CarId INT NOT NULL FOREIGN KEY REFERENCES Cars(CarId),
    ReservationDate DATETIME DEFAULT GETDATE(),
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    TotalPrice DECIMAL(10, 2) NOT NULL,
    Status NVARCHAR(20) CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled', 'Completed')) DEFAULT 'Pending'
);

-- Bảng Payments
CREATE TABLE Payments (
    PaymentId INT IDENTITY(1,1) PRIMARY KEY,
    ReservationId INT NOT NULL FOREIGN KEY REFERENCES Reservations(ReservationId),
    PaymentDate DATETIME DEFAULT GETDATE(),
    Amount DECIMAL(10, 2) NOT NULL,
    PaymentMethod NVARCHAR(20) CHECK (PaymentMethod IN ('Credit Card', 'Cash', 'Online')),
    Status NVARCHAR(20) CHECK (Status IN ('Paid', 'Unpaid')) DEFAULT 'Unpaid'
);

-- Bảng MaintenanceRecords
CREATE TABLE MaintenanceRecords (
    RecordId INT IDENTITY(1,1) PRIMARY KEY,
    CarId INT NOT NULL FOREIGN KEY REFERENCES Cars(CarId),
    MaintenanceDate DATETIME DEFAULT GETDATE(),
    Description NVARCHAR(MAX),
    Cost DECIMAL(10, 2)
);

-- Bảng Reviews
CREATE TABLE Reviews (
    ReviewId INT IDENTITY(1,1) PRIMARY KEY,
    CarId INT NOT NULL FOREIGN KEY REFERENCES Cars(CarId),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    Rating INT CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(MAX),
    ReviewDate DATETIME DEFAULT GETDATE()
);

-- Bảng Promotions
CREATE TABLE Promotions (
    PromotionId INT IDENTITY(1,1) PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(MAX),
    DiscountPercent DECIMAL(5, 2) CHECK (DiscountPercent BETWEEN 0 AND 100),
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    Status NVARCHAR(20) CHECK (Status IN ('Active', 'Inactive')) DEFAULT 'Active'
);

-- Bảng Blog
CREATE TABLE Blog (
    BlogId INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    ImageUrl NVARCHAR(255),
    AuthorId INT NOT NULL FOREIGN KEY REFERENCES Users(UserId),
    PublishedDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) CHECK (Status IN ('Draft', 'Published')) DEFAULT 'Draft'
);

-- Bảng Contact
CREATE TABLE Contact (
    ContactId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PhoneNumber NVARCHAR(15),
    Message NVARCHAR(MAX) NOT NULL,
    SubmittedDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) CHECK (Status IN ('Pending', 'Processed')) DEFAULT 'Pending'
);