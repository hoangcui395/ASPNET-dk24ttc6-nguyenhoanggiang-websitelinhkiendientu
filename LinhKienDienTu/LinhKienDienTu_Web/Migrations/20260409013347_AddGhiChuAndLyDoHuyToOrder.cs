using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinhKienDienTu_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddGhiChuAndLyDoHuyToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chỉ thêm 2 cột mới — bỏ các lệnh Drop/Create FK vì schema DB thực tế khác với snapshot
            migrationBuilder.AddColumn<string>(
                name: "Ghi_Chu",
                table: "Order",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ly_Do_Huy",
                table: "Order",
                type: "nvarchar(max)",
                nullable: true);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_User_User_ID",
                table: "Order");

            migrationBuilder.DropIndex(
                name: "IX_Order_User_ID",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Ghi_Chu",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Ly_Do_Huy",
                table: "Order");

            migrationBuilder.AlterColumn<string>(
                name: "Hinh_Anh",
                table: "Product",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "User_ID1",
                table: "Order",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Order_User_ID1",
                table: "Order",
                column: "User_ID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Order_User_User_ID1",
                table: "Order",
                column: "User_ID1",
                principalTable: "User",
                principalColumn: "User_ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
