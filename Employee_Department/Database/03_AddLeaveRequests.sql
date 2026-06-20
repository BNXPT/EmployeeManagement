-- =================================================================
-- เพิ่มตาราง LeaveRequests สำหรับระบบลางาน
-- =================================================================
USE EmployeeManagementDB;
GO

IF OBJECT_ID(N'dbo.LeaveRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeaveRequests
    (
        LeaveRequestId         INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId             INT NOT NULL,
        LeaveType              NVARCHAR(50)  NOT NULL,
        Reason                 NVARCHAR(500) NOT NULL,
        StartDate              DATETIME2     NOT NULL,
        EndDate                DATETIME2     NOT NULL,
        Status                 NVARCHAR(20)  NOT NULL DEFAULT('Pending'),  -- Pending|Approved|Rejected
        RequestedAt            DATETIME2     NOT NULL DEFAULT(GETDATE()),
        RespondedAt            DATETIME2     NULL,
        ManagerComment         NVARCHAR(500) NULL,
        ApprovalToken          NVARCHAR(64)  NOT NULL UNIQUE,
        RespondedByManagerId   INT NULL,
        CONSTRAINT FK_LeaveRequests_Employees FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees(EmployeeId) ON DELETE CASCADE,
        CONSTRAINT FK_LeaveRequests_Managers FOREIGN KEY (RespondedByManagerId)
            REFERENCES dbo.Managers(ManagerId) ON DELETE SET NULL
    );
END
GO

PRINT N'สร้างตาราง LeaveRequests เรียบร้อย';
GO
