/*
    ComplaintTracker schema.

    Creates the database and the Complaints table if they are not already
    there. Safe to run repeatedly: every step checks first, nothing is
    dropped, and existing rows are left alone.

    Run with:
        sqlcmd -S localhost -E -i Database\schema.sql
*/

IF DB_ID('ComplaintTrackerDb') IS NULL
BEGIN
    PRINT 'Creating database ComplaintTrackerDb.';
    CREATE DATABASE ComplaintTrackerDb;
END
ELSE
BEGIN
    PRINT 'Database ComplaintTrackerDb already exists, leaving it as is.';
END
GO

USE ComplaintTrackerDb;
GO

IF OBJECT_ID('dbo.Complaints', 'U') IS NULL
BEGIN
    PRINT 'Creating table dbo.Complaints.';

    CREATE TABLE dbo.Complaints
    (
        Id          INT            IDENTITY(1,1) NOT NULL,
        Title       NVARCHAR(200)  NOT NULL,
        Description NVARCHAR(MAX)  NOT NULL,
        Category    NVARCHAR(100)  NOT NULL,
        Status      NVARCHAR(50)   NOT NULL,
        CreatedDate DATETIME2      NOT NULL,
        RaisedBy    NVARCHAR(100)  NOT NULL,

        CONSTRAINT PK_Complaints PRIMARY KEY CLUSTERED (Id)
    );
END
ELSE
BEGIN
    PRINT 'Table dbo.Complaints already exists, leaving it as is.';
END
GO

/*
    Status is a fixed set. The API rejects anything else with a 400, and this
    constraint stops a bad value arriving by any other route. Kept in step with
    the AllowedValues attribute on Complaint.Status; change both together.
*/
IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_Complaints_Status'
      AND parent_object_id = OBJECT_ID('dbo.Complaints')
)
BEGIN
    PRINT 'Adding constraint CK_Complaints_Status.';

    ALTER TABLE dbo.Complaints
        ADD CONSTRAINT CK_Complaints_Status
            CHECK (Status IN ('Open', 'In Progress', 'Resolved', 'Closed'));
END
ELSE
BEGIN
    PRINT 'Constraint CK_Complaints_Status already exists, leaving it as is.';
END
GO
