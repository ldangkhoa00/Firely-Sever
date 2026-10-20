BEGIN TRANSACTION;
CREATE UNIQUE INDEX [IX_Users_HospitalEmail] ON [Users] ([HospitalEmail]) WHERE [HospitalEmail] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261014165338_AddUniqueHospitalEmailIndex', N'10.0.12');

COMMIT;
GO

