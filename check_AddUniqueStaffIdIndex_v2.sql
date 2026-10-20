BEGIN TRANSACTION;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.Users')
            AND name = N'IX_Users_StaffId'
        )
        BEGIN
            CREATE UNIQUE INDEX [IX_Users_StaffId]
            ON [dbo].[Users] ([StaffId]);

            EXEC sys.sp_addextendedproperty
                @name = N'CreatedBy_AddUniqueStaffIdIndex',
                @value = 1,
                @level0type = N'SCHEMA',
                @level0name = N'dbo',
                @level1type = N'TABLE',
                @level1name = N'Users',
                @level2type = N'INDEX',
                @level2name = N'IX_Users_StaffId';
        END
        

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261014134452_AddUniqueStaffIdIndex', N'10.0.12');

COMMIT;
GO

