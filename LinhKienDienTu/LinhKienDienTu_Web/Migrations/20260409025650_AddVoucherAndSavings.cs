using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinhKienDienTu_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddVoucherAndSavings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "User",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Tien_Tiet_Kiem_Khuyen_Mai",
                table: "Order",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tien_Tiet_Kiem_Voucher",
                table: "Order",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Voucher_ID",
                table: "Order",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Voucher",
                columns: table => new
                {
                    Voucher_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma_Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Loai_Giam_Gia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Gia_Tri_Giam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    So_Luong = table.Column<int>(type: "int", nullable: false),
                    Ngay_Het_Han = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voucher", x => x.Voucher_ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Order_Voucher_ID",
                table: "Order",
                column: "Voucher_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Voucher_Voucher_ID",
                table: "Order",
                column: "Voucher_ID",
                principalTable: "Voucher",
                principalColumn: "Voucher_ID",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Voucher_Voucher_ID",
                table: "Order");

            migrationBuilder.DropTable(
                name: "Voucher");

            migrationBuilder.DropIndex(
                name: "IX_Order_Voucher_ID",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Tien_Tiet_Kiem_Khuyen_Mai",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Tien_Tiet_Kiem_Voucher",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "Voucher_ID",
                table: "Order");
        }
    }
}
