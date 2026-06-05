IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[TriColorLampDevice]') AND type IN ('U'))
BEGIN
    CREATE TABLE [dbo].[TriColorLampDevice] (
        [Id] nvarchar(50) NOT NULL,
        [ProjectState] int NOT NULL,
        [ProjectType] nvarchar(100) NULL,
        [DtuId] bigint NOT NULL,
        [DtuSn] nvarchar(100) NOT NULL,
        [DeviceName] nvarchar(200) NULL,
        [DeviceId] bigint NOT NULL,
        [UpdateTime] datetime NOT NULL,
        CONSTRAINT [PK_TriColorLampDevice] PRIMARY KEY NONCLUSTERED ([Id])
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_TriColorLampDevice_DtuSn]
        ON [dbo].[TriColorLampDevice] ([DtuSn]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[TriColorLampDailyLampData]') AND type IN ('U'))
BEGIN
    CREATE TABLE [dbo].[TriColorLampDailyLampData] (
        [Id] nvarchar(50) NOT NULL,
        [DataDate] nvarchar(20) NOT NULL,
        [DtuSn] nvarchar(100) NOT NULL,
        [DeviceName] nvarchar(200) NULL,
        [LampState] int NOT NULL,
        [StartTime] datetime NULL,
        [EndTime] datetime NULL,
        [Duration] bigint NOT NULL,
        [UpdateTime] datetime NOT NULL,
        CONSTRAINT [PK_TriColorLampDailyLampData] PRIMARY KEY NONCLUSTERED ([Id])
    );

    CREATE NONCLUSTERED INDEX [IX_TriColorLampDailyLampData_Date_DtuSn]
        ON [dbo].[TriColorLampDailyLampData] ([DataDate], [DtuSn]);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[OpenJob] WHERE [Id] = N'tricolor-lamp-daily-data-sync')
BEGIN
    INSERT INTO [dbo].[OpenJob]
        ([Id], [JobName], [RunCount], [ErrorCount], [NextRunTime], [LastRunTime], [LastErrorTime], [JobType], [JobCall], [JobCallParams], [Cron], [Status], [Remark], [CreateTime], [CreateUserId], [CreateUserName], [UpdateTime], [UpdateUserId], [UpdateUserName], [OrgId])
    VALUES
        (N'tricolor-lamp-daily-data-sync', N'TriColorLamp device and daily lamp data sync', 0, 0, GETDATE(), GETDATE(), GETDATE(), 0, N'OpenAuth.App.Jobs.TriColorLampDailyDataSyncJob', N'null', N'0 0/1 * * * ?', 1, N'Sync all tri-color lamp devices and today lamp data every 1 minute for testing.', GETDATE(), N'00000000-0000-0000-0000-000000000000', N'Admin', GETDATE(), N'00000000-0000-0000-0000-000000000000', N'Admin', N'');
END
GO
