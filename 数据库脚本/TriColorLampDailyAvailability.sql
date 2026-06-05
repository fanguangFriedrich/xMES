IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[TriColorLampDailyAvailability]') AND type IN ('U'))
BEGIN
    CREATE TABLE [dbo].[TriColorLampDailyAvailability] (
        [Id] nvarchar(50) NOT NULL,
        [DataDate] nvarchar(20) NOT NULL,
        [DtuSn] nvarchar(100) NOT NULL,
        [DeviceName] nvarchar(200) NULL,
        [WorkStartTime] datetime NOT NULL,
        [WorkEndTime] datetime NOT NULL,
        [MaintenanceStartTime] datetime NULL,
        [MaintenanceEndTime] datetime NULL,
        [WorkDuration] bigint NOT NULL,
        [MaintenanceOverlapDuration] bigint NOT NULL,
        [AvailableWorkDuration] bigint NOT NULL,
        [RedDuration] bigint NOT NULL,
        [YellowDuration] bigint NOT NULL,
        [GreenDuration] bigint NOT NULL,
        [BlueDuration] bigint NOT NULL,
        [OffDuration] bigint NOT NULL,
        [OtherDuration] bigint NOT NULL,
        [AvailabilityRate] decimal(18, 2) NOT NULL,
        [UpdateTime] datetime NOT NULL,
        CONSTRAINT [PK_TriColorLampDailyAvailability] PRIMARY KEY NONCLUSTERED ([Id])
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_TriColorLampDailyAvailability_Date_DtuSn]
        ON [dbo].[TriColorLampDailyAvailability] ([DataDate], [DtuSn]);
END
GO
