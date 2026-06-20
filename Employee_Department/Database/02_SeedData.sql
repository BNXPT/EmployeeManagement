-- =================================================================
-- เติมข้อมูลตัวอย่าง: 10 แผนก, 10 หัวหน้า, 100 พนักงาน, Admin
-- =================================================================
USE EmployeeManagementDB;
GO

-- ล้างข้อมูลเดิม (ถ้ามี)
DELETE FROM dbo.Users;
DELETE FROM dbo.Employees;
DELETE FROM dbo.Managers;
DELETE FROM dbo.Departments;
DBCC CHECKIDENT ('dbo.Users', RESEED, 0);
DBCC CHECKIDENT ('dbo.Employees', RESEED, 0);
DBCC CHECKIDENT ('dbo.Managers', RESEED, 0);
DBCC CHECKIDENT ('dbo.Departments', RESEED, 0);
GO

-- 1) แผนก
INSERT INTO dbo.Departments (DepartmentName) VALUES
(N'ฝ่ายบุคคล'), (N'ฝ่ายบัญชี'), (N'ฝ่ายการตลาด'),
(N'ฝ่ายไอที'), (N'ฝ่ายขาย'), (N'ฝ่ายผลิต'),
(N'ฝ่ายจัดซื้อ'), (N'ฝ่ายคลังสินค้า'), (N'ฝ่ายวิจัยและพัฒนา'),
(N'ฝ่ายกฎหมาย');
GO

-- 2) หัวหน้าแผนก (1 คน/แผนก)
INSERT INTO dbo.Managers (FullName, NationalId, DepartmentId, PhoneNumber, Email) VALUES
(N'นายสมชาย ใจดี',         N'1100000000000', 1, N'0801234567', N'manager1@company.com'),
(N'นางสาวสุดา รักงาน',     N'1100000007919', 2, N'0811234567', N'manager2@company.com'),
(N'นายวิชัย ขยันมาก',      N'1100000015838', 3, N'0821234567', N'manager3@company.com'),
(N'นางสาวพรทิพย์ สุขใจ',   N'1100000023757', 4, N'0831234567', N'manager4@company.com'),
(N'นายธนากร มั่งมี',       N'1100000031676', 5, N'0841234567', N'manager5@company.com'),
(N'นางสาวอรอนงค์ ดีงาม',   N'1100000039595', 6, N'0851234567', N'manager6@company.com'),
(N'นายประยุทธ์ พัฒนา',     N'1100000047514', 7, N'0861234567', N'manager7@company.com'),
(N'นางสาวมณีรัตน์ เพชรงาม', N'1100000055433', 8, N'0871234567', N'manager8@company.com'),
(N'นายอนุชา สร้างสรรค์',   N'1100000063352', 9, N'0881234567', N'manager9@company.com'),
(N'นางสาวกัลยา ยุติธรรม',  N'1100000071271', 10, N'0891234567', N'manager10@company.com');
GO

-- 3) พนักงาน (10 คน/แผนก = 100 คน)
DECLARE @i INT = 0;
DECLARE @deptId INT;
DECLARE @base BIGINT = 2100000000000;
DECLARE @firstNames TABLE (N NVARCHAR(50));
INSERT INTO @firstNames VALUES (N'จักรพงษ์'),(N'ปรียา'),(N'รัตนา'),(N'วิลาวัลย์'),
(N'ณรงค์'),(N'นพดล'),(N'สุรชัย'),(N'ธวัชชัย'),(N'พิทักษ์'),(N'อนันต์');

DECLARE @lastNames TABLE (N NVARCHAR(50));
INSERT INTO @lastNames VALUES (N'ทองดี'),(N'เจริญรัตน์'),(N'ทรงศักดิ์'),(N'อยู่ดี'),
(N'สวัสดี'),(N'ศรีสุข'),(N'บุญมี'),(N'สุวรรณ'),(N'มีสุข'),(N'ใจกล้า');

DECLARE @positions TABLE (N NVARCHAR(50));
INSERT INTO @positions VALUES (N'เจ้าหน้าที่'),(N'เจ้าหน้าที่ปฏิบัติการ'),
(N'ผู้ช่วย'),(N'นักวิเคราะห์'),(N'พนักงาน'),(N'เจ้าหน้าที่อาวุโส');

DECLARE @j INT;
DECLARE @nationalId NVARCHAR(13);
DECLARE @fname NVARCHAR(100);
DECLARE @lname NVARCHAR(100);
DECLARE @pos NVARCHAR(100);

DECLARE deptCursor CURSOR FOR
    SELECT DepartmentId FROM dbo.Departments ORDER BY DepartmentId;
OPEN deptCursor;
FETCH NEXT FROM deptCursor INTO @deptId;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @j = 0;
    WHILE @j < 10
    BEGIN
        SET @nationalId = RIGHT(REPLICATE('0',13) + CAST(@base + @i AS NVARCHAR), 13);
        SELECT TOP 1 @fname = N FROM @firstNames ORDER BY NEWID();
        SELECT TOP 1 @lname = N FROM @lastNames ORDER BY NEWID();
        SELECT TOP 1 @pos = N FROM @positions ORDER BY NEWID();

        INSERT INTO dbo.Employees (FullName, NationalId, DepartmentId, Position, PhoneNumber, Email)
        VALUES (@fname + N' ' + @lname, @nationalId, @deptId, @pos,
                N'09' + RIGHT('00000000' + CAST(ABS(CHECKSUM(NEWID())) % 100000000 AS NVARCHAR), 8),
                N'emp' + CAST(@i + 1 AS NVARCHAR) + N'@company.com');

        SET @i = @i + 1;
        SET @j = @j + 1;
    END
    FETCH NEXT FROM deptCursor INTO @deptId;
END
CLOSE deptCursor;
DEALLOCATE deptCursor;
GO

-- 4) บัญชีผู้ใช้
-- Admin
INSERT INTO dbo.Users (Username, Password, FullName, Role) VALUES
(N'admin',  N'admin111', N'ผู้ดูแลระบบ',    N'Admin'),
(N'admin2', N'admin222', N'ผู้ดูแลระบบ 2', N'Admin'),
(N'admin3', N'admin333', N'ผู้ดูแลระบบ 3', N'Admin');

-- Manager accounts
INSERT INTO dbo.Users (Username, Password, FullName, Role, ManagerId)
SELECT N'manager' + CAST(ROW_NUMBER() OVER (ORDER BY ManagerId) AS NVARCHAR),
       N'manager' + CAST(ROW_NUMBER() OVER (ORDER BY ManagerId) AS NVARCHAR),
       FullName, N'Manager', ManagerId
FROM dbo.Managers;

-- Employee accounts
INSERT INTO dbo.Users (Username, Password, FullName, Role, EmployeeId)
SELECT N'emp' + CAST(ROW_NUMBER() OVER (ORDER BY EmployeeId) AS NVARCHAR),
       N'emp' + CAST(ROW_NUMBER() OVER (ORDER BY EmployeeId) AS NVARCHAR),
       FullName, N'Employee', EmployeeId
FROM dbo.Employees;
GO

PRINT N'เพิ่มข้อมูลตัวอย่างเรียบร้อยแล้ว';
GO
