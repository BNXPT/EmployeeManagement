USE EmployeeManagementDB;
GO

DECLARE @sql NVARCHAR(MAX) = N'';

-- เพิ่ม IsDeleted และ DeletedAt ในทุกตาราง
DECLARE @tables TABLE (TableName SYSNAME);
INSERT INTO @tables VALUES
('Departments'), ('Employees'), ('Managers'),
('Users'), ('Positions'), ('LeaveRequests');

DECLARE @TableName SYSNAME;
DECLARE table_cursor CURSOR FOR SELECT TableName FROM @tables;
OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @TableName;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- เพิ่ม IsDeleted ถ้ายังไม่มี
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE Name = N'IsDeleted' AND Object_ID = OBJECT_ID(@TableName))
    BEGIN
        SET @sql = N'ALTER TABLE dbo.[' + @TableName + N'] ADD IsDeleted BIT NOT NULL DEFAULT(0)';
        EXEC sp_executesql @sql;
        PRINT N'เพิ่ม IsDeleted ใน ' + @TableName;
    END

    -- เพิ่ม DeletedAt ถ้ายังไม่มี
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE Name = N'DeletedAt' AND Object_ID = OBJECT_ID(@TableName))
    BEGIN
        SET @sql = N'ALTER TABLE dbo.[' + @TableName + N'] ADD DeletedAt DATETIME2 NULL';
        EXEC sp_executesql @sql;
        PRINT N'เพิ่ม DeletedAt ใน ' + @TableName;
    END

    FETCH NEXT FROM table_cursor INTO @TableName;
END
CLOSE table_cursor;
DEALLOCATE table_cursor;

PRINT N'เสร็จเรียบร้อย — ทุกตารางมี IsDeleted และ DeletedAt แล้ว';
GO
