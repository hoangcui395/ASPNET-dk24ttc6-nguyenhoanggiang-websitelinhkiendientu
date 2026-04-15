using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinhKienDienTu_Web.Migrations
{
    /// <inheritdoc />
    public partial class AdminEnhancementsFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "So_Luong",
                table: "Voucher",
                newName: "So_Luong_Gioi_Han");

            migrationBuilder.AddColumn<decimal>(
                name: "Don_Toi_Thieu",
                table: "Voucher",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "Ngay_Bat_Dau",
                table: "Voucher",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "So_Luong_Da_Dung",
                table: "Voucher",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Trang_Thai",
                table: "Voucher",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTphcmOnly",
                table: "Product",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Gift_Rule",
                columns: table => new
                {
                    GiftRule_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MainProduct_ID = table.Column<int>(type: "int", nullable: false),
                    MinQuantity = table.Column<int>(type: "int", nullable: false),
                    GiftProduct_ID = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gift_Rule", x => x.GiftRule_ID);
                    table.ForeignKey(
                        name: "FK_Gift_Rule_Product_GiftProduct_ID",
                        column: x => x.GiftProduct_ID,
                        principalTable: "Product",
                        principalColumn: "Product_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gift_Rule_Product_MainProduct_ID",
                        column: x => x.MainProduct_ID,
                        principalTable: "Product",
                        principalColumn: "Product_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gift_Rule_GiftProduct_ID",
                table: "Gift_Rule",
                column: "GiftProduct_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Gift_Rule_MainProduct_ID",
                table: "Gift_Rule",
                column: "MainProduct_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gift_Rule");

            migrationBuilder.DropColumn(
                name: "Don_Toi_Thieu",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "Ngay_Bat_Dau",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "So_Luong_Da_Dung",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "Trang_Thai",
                table: "Voucher");

            migrationBuilder.DropColumn(
                name: "IsTphcmOnly",
                table: "Product");

            migrationBuilder.RenameColumn(
                name: "So_Luong_Gioi_Han",
                table: "Voucher",
                newName: "So_Luong");
        }
    }
}
