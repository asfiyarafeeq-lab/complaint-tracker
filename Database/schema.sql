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

/*
    Indexes for the columns the listing endpoint filters and sorts on. Without
    them every request reads the whole table. Each is created only when it is
    missing, so this section is as safe to re-run as the rest of the file.

    Title search uses LIKE '%keyword%', which cannot use an index at all; the
    index below serves sortBy=title instead.
*/

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Complaints_Status'
                 AND object_id = OBJECT_ID('dbo.Complaints'))
BEGIN
    PRINT 'Creating index IX_Complaints_Status.';
    CREATE NONCLUSTERED INDEX IX_Complaints_Status ON dbo.Complaints (Status);
END
ELSE
BEGIN
    PRINT 'Index IX_Complaints_Status already exists, leaving it as is.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Complaints_Category'
                 AND object_id = OBJECT_ID('dbo.Complaints'))
BEGIN
    PRINT 'Creating index IX_Complaints_Category.';
    CREATE NONCLUSTERED INDEX IX_Complaints_Category ON dbo.Complaints (Category);
END
ELSE
BEGIN
    PRINT 'Index IX_Complaints_Category already exists, leaving it as is.';
END
GO

-- Declared DESC to match the default ordering, though SQL Server can read an
-- index backwards when the query asks for the opposite direction.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Complaints_CreatedDate'
                 AND object_id = OBJECT_ID('dbo.Complaints'))
BEGIN
    PRINT 'Creating index IX_Complaints_CreatedDate.';
    CREATE NONCLUSTERED INDEX IX_Complaints_CreatedDate ON dbo.Complaints (CreatedDate DESC);
END
ELSE
BEGIN
    PRINT 'Index IX_Complaints_CreatedDate already exists, leaving it as is.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Complaints_Title'
                 AND object_id = OBJECT_ID('dbo.Complaints'))
BEGIN
    PRINT 'Creating index IX_Complaints_Title.';
    CREATE NONCLUSTERED INDEX IX_Complaints_Title ON dbo.Complaints (Title);
END
ELSE
BEGIN
    PRINT 'Index IX_Complaints_Title already exists, leaving it as is.';
END
GO

/*
    Accounts that may use the system. Passwords are stored only as a hash
    produced by the API; nothing here can be read back as a password.

    Role is a fixed set, kept in step with UserRoles in the API. New accounts
    are always created as 'User'; promote one deliberately with:

        UPDATE dbo.Users SET Role = 'Admin' WHERE Username = 'you@example.com';
*/
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    PRINT 'Creating table dbo.Users.';

    CREATE TABLE dbo.Users
    (
        Id           INT            IDENTITY(1,1) NOT NULL,
        Username     NVARCHAR(100)  NOT NULL,
        PasswordHash NVARCHAR(500)  NOT NULL,
        Role         NVARCHAR(20)   NOT NULL,
        CreatedDate  DATETIME2      NOT NULL,

        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Users_Role CHECK (Role IN ('User', 'Agent', 'Admin'))
    );
END
ELSE
BEGIN
    PRINT 'Table dbo.Users already exists, leaving it as is.';
END
GO

-- Usernames must be unique: two accounts with the same name would make login
-- ambiguous. Unique index rather than a constraint so the lookup is indexed too.
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_Users_Username'
                 AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    PRINT 'Creating unique index UX_Users_Username.';
    CREATE UNIQUE NONCLUSTERED INDEX UX_Users_Username ON dbo.Users (Username);
END
ELSE
BEGIN
    PRINT 'Index UX_Users_Username already exists, leaving it as is.';
END
GO

/*
    Links a complaint to the account that raised it, so a User can be shown
    only their own. Nullable because rows created before accounts existed have
    no owner; those stay visible to Agents and Admins only.
*/
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE name = 'RaisedByUserId'
                 AND object_id = OBJECT_ID('dbo.Complaints'))
BEGIN
    PRINT 'Adding column dbo.Complaints.RaisedByUserId.';

    ALTER TABLE dbo.Complaints ADD RaisedByUserId INT NULL;
END
ELSE
BEGIN
    PRINT 'Column RaisedByUserId already exists, leaving it as is.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
               WHERE name = 'FK_Complaints_RaisedByUser')
BEGIN
    PRINT 'Adding foreign key FK_Complaints_RaisedByUser.';

    ALTER TABLE dbo.Complaints
        ADD CONSTRAINT FK_Complaints_RaisedByUser
            FOREIGN KEY (RaisedByUserId) REFERENCES dbo.Users (Id);
END
ELSE
BEGIN
    PRINT 'Foreign key FK_Complaints_RaisedByUser already exists, leaving it as is.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Complaints_RaisedByUserId'
                 AND object_id = OBJECT_ID('dbo.Complaints'))
BEGIN
    PRINT 'Creating index IX_Complaints_RaisedByUserId.';
    CREATE NONCLUSTERED INDEX IX_Complaints_RaisedByUserId ON dbo.Complaints (RaisedByUserId);
END
ELSE
BEGIN
    PRINT 'Index IX_Complaints_RaisedByUserId already exists, leaving it as is.';
END
GO

/*
    Who is working the ticket. Null means nobody has picked it up yet, which is
    how unassigned work is found. AssignedTo holds the name for display, the
    same way RaisedBy does.
*/
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE name = 'AssignedToUserId'
                 AND object_id = OBJECT_ID('dbo.Complaints'))
BEGIN
    PRINT 'Adding columns AssignedToUserId and AssignedTo.';

    ALTER TABLE dbo.Complaints ADD AssignedToUserId INT NULL;
END
ELSE
BEGIN
    PRINT 'Column AssignedToUserId already exists, leaving it as is.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE name = 'AssignedTo'
                 AND object_id = OBJECT_ID('dbo.Complaints'))
BEGIN
    ALTER TABLE dbo.Complaints ADD AssignedTo NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
               WHERE name = 'FK_Complaints_AssignedToUser')
BEGIN
    PRINT 'Adding foreign key FK_Complaints_AssignedToUser.';

    ALTER TABLE dbo.Complaints
        ADD CONSTRAINT FK_Complaints_AssignedToUser
            FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id);
END
ELSE
BEGIN
    PRINT 'Foreign key FK_Complaints_AssignedToUser already exists, leaving it as is.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Complaints_AssignedToUserId'
                 AND object_id = OBJECT_ID('dbo.Complaints'))
BEGIN
    PRINT 'Creating index IX_Complaints_AssignedToUserId.';
    CREATE NONCLUSTERED INDEX IX_Complaints_AssignedToUserId ON dbo.Complaints (AssignedToUserId);
END
ELSE
BEGIN
    PRINT 'Index IX_Complaints_AssignedToUserId already exists, leaving it as is.';
END
GO
