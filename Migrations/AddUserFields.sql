-- Migration: AddUserFields
-- Add new columns to AspNetUsers table

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'Gender')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [Gender] nvarchar(max) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'DateOfBirth')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [DateOfBirth] datetime2 NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'Address')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [Address] nvarchar(200) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'PhoneNumber2')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [PhoneNumber2] nvarchar(max) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [CreatedAt] datetime2 NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AspNetUsers]') AND name = 'UpdatedAt')
BEGIN
    ALTER TABLE [dbo].[AspNetUsers]
    ADD [UpdatedAt] datetime2 NULL;
END
GO



