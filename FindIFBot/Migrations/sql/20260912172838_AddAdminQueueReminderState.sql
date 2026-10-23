-- Equivalent to migration 20260912172838_AddAdminQueueReminderState
IF OBJECT_ID(N'[dbo].[AdminQueueReminderStates]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AdminQueueReminderStates] (
        [Id] int NOT NULL,
        [DueAtUtc] datetime2 NULL,
        [LastSentAtUtc] datetime2 NULL,
        CONSTRAINT [PK_AdminQueueReminderStates] PRIMARY KEY ([Id])
    );
END
GO
