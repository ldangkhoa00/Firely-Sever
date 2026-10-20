BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [HospitalEmail] nvarchar(254) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261014163254_AddHospitalEmailToUsers', N'10.0.12');

COMMIT;
GO

