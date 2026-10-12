BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [TokenVersion] int NOT NULL DEFAULT 1;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261011104527_AddTokenVersionToUsers', N'10.0.12');

COMMIT;
GO

