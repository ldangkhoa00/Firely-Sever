using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalUsers.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueHospitalEmailIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_HospitalEmail",
                table: "Users",
                column: "HospitalEmail",
                unique: true,
                filter: "[HospitalEmail] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_HospitalEmail",
                table: "Users");
        }
    }
}
