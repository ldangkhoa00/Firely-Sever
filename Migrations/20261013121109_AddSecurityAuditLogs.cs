
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalUsers.Migrations
{
    public partial class AddSecurityAuditLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chỉ tạo bảng nhật ký bảo mật
            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(
                        type: "bigint",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    ActorStaffId = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false),

                    TargetStaffId = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false),

                    Action = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: false),

                    Result = table.Column<string>(
                        type: "nvarchar(20)",
                        maxLength: 20,
                        nullable: false),

                    CreatedAtUtc = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_SecurityAuditLogs",
                        x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Chỉ đảo ngược việc tạo bảng nhật ký
            migrationBuilder.DropTable(
                name: "SecurityAuditLogs");
        }
    }
}
