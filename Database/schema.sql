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
