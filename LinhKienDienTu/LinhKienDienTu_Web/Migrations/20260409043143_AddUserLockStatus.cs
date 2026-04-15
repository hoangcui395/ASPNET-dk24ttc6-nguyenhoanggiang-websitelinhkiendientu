using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinhKienDienTu_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLockStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "User");
        }
    }
}
