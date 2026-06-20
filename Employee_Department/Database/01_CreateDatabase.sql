-- =================================================================
-- ระบบจัดการแผนกพนักงาน - สคริปต์สร้างฐานข้อมูล
-- ใช้กับ SQL Server Management Studio 22
-- =================================================================

-- 1) สร้างฐานข้อมูล (รันที่ master)
IF DB_ID(N'EmployeeManagementDB') IS NULL
BEGIN
    CREATE DATABASE EmployeeManagementDB;
END
GO

USE EmployeeManagementDB;
GO

-- 2) ตาราง Departments (แผนก)
IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments
    (
        DepartmentId    INT IDENTITY(1,1) PRIMARY KEY,
        DepartmentName  NVARCHAR(100) NOT NULL
    );
END
GO

-- 3) ตาราง Managers (หัวหน้าแผนก - 1 คน ต่อ 1 แผนก)
IF OBJECT_ID(N'dbo.Managers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Managers
    (
        ManagerId       INT IDENTITY(1,1) PRIMARY KEY,
        FullName        NVARCHAR(100) NOT NULL,
        NationalId      NVARCHAR(13)  NOT NULL UNIQUE,
        DepartmentId    INT           NOT NULL UNIQUE,
        PhoneNumber     NVARCHAR(15)  NOT NULL,
        Email           NVARCHAR(150) NOT NULL,
        CONSTRAINT FK_Managers_Departments FOREIGN KEY (DepartmentId)
            REFERENCES dbo.Departments(DepartmentId)
    );
END
GO

-- 4) ตาราง Employees (พนักงาน - 10 คนต่อแผนก)
IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employees
    (
        EmployeeId      INT IDENTITY(1,1) PRIMARY KEY,
        FullName        NVARCHAR(100) NOT NULL,
        NationalId      NVARCHAR(13)  NOT NULL UNIQUE,
        DepartmentId    INT           NOT NULL,
        Position        NVARCHAR(100) NOT NULL,
        PhoneNumber     NVARCHAR(15)  NOT NULL,
        Email           NVARCHAR(150) NOT NULL,
        CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId)
            REFERENCES dbo.Departments(DepartmentId)
    );
END
GO

-- 5) ตาราง Users (บัญชีผู้ใช้: Admin, Manager, Employee)
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId      INT IDENTITY(1,1) PRIMARY KEY,
        Username    NVARCHAR(50)  NOT NULL UNIQUE,
        Password    NVARCHAR(255) NOT NULL,
        FullName    NVARCHAR(100) NOT NULL,
        Role        NVARCHAR(20)  NOT NULL,   -- Admin | Manager | Employee
        EmployeeId  INT NULL,
        ManagerId   INT NULL
    );
END
GO

PRINT N'สร้างฐานข้อมูล EmployeeManagementDB และตารางทั้งหมดเรียบร้อยแล้ว';
GO
