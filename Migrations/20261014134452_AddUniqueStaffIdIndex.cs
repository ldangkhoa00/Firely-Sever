using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalUsers.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueStaffIdIndex : Migration
    {
        /// <inheritdoc />
       
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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
        ");
        }


        /// <inheritdoc />
        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        IF EXISTS (
            SELECT 1
            FROM sys.indexes AS i
            INNER JOIN sys.extended_properties AS ep
                ON ep.class = 7
            AND ep.major_id = i.object_id
            AND ep.minor_id = i.index_id
            WHERE i.object_id = OBJECT_ID(N'dbo.Users')
            AND i.name = N'IX_Users_StaffId'
            AND ep.name = N'CreatedBy_AddUniqueStaffIdIndex'
        )
        BEGIN
            DROP INDEX [IX_Users_StaffId]
            ON [dbo].[Users];
        END
        ");
        }
    }
}
