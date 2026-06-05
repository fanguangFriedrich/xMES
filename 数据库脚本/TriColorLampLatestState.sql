IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[TriColorLampLatestState]') AND type IN ('U'))
BEGIN
    CREATE TABLE [dbo].[TriColorLampLatestState] (
        [Id] nvarchar(50) NOT NULL,
        [DtuSn] nvarchar(100) NOT NULL,
        [DeviceName] nvarchar(200) NULL,
        [LampState] int NOT NULL,
        [StartTime] datetime NULL,
        [UpdateTime] datetime NOT NULL,
        CONSTRAINT [PK_TriColorLampLatestState] PRIMARY KEY NONCLUSTERED ([Id])
    );

    CREATE UNIQUE NONCLUSTERED INDEX [IX_TriColorLampLatestState_DtuSn]
        ON [dbo].[TriColorLampLatestState] ([DtuSn]);
END
GO
