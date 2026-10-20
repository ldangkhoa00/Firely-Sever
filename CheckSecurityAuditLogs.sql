BEGIN TRANSACTION;
CREATE TABLE [SecurityAuditLogs] (
    [Id] bigint NOT NULL IDENTITY,
    [ActorStaffId] nvarchar(20) NOT NULL,
    [TargetStaffId] nvarchar(20) NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [Result] nvarchar(20) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_SecurityAuditLogs] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261013121109_AddSecurityAuditLogs', N'10.0.12');

COMMIT;
GO

