BEGIN TRANSACTION;
CREATE UNIQUE INDEX [IX_Users_StaffId] ON [Users] ([StaffId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261014134452_AddUniqueStaffIdIndex', N'10.0.12');

COMMIT;
GO

