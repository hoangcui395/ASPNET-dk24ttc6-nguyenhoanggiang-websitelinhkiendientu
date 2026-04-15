using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinhKienDienTu_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddTrangThaiDonHangToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chỉ thêm cột mới — bỏ Drop/Rename/Index vì schema DB thực tế khác với EF snapshot
            migrationBuilder.AddColumn<string>(
                name: "Trang_Thai_Don_Hang",
                table: "Order",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "Chờ duyệt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Order_Detail_Order_Order_ID",
                table: "Order_Detail");

            migrationBuilder.DropForeignKey(
                name: "FK_Order_Detail_Product_Product_ID",
                table: "Order_Detail");

            migrationBuilder.DropIndex(
                name: "IX_Order_Detail_Order_ID",
                table: "Order_Detail");

            migrationBuilder.DropIndex(
                name: "IX_Order_Detail_Product_ID",
                table: "Order_Detail");

            migrationBuilder.DropColumn(
                name: "Thanh_Tien",
                table: "Order_Detail");

            migrationBuilder.DropColumn(
                name: "Trang_Thai_Don_Hang",
                table: "Order");

            migrationBuilder.RenameColumn(
                name: "Don_Gia",
                table: "Order_Detail",
                newName: "Gia_Ban");

            migrationBuilder.RenameColumn(
                name: "Order_Detail_ID",
                table: "Order_Detail",
                newName: "OrderDetail_ID");

            migrationBuilder.AddColumn<int>(
                name: "Order_ID1",
                table: "Order_Detail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Product_ID1",
                table: "Order_Detail",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Order_Detail_Order_ID1",
                table: "Order_Detail",
                column: "Order_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Detail_Product_ID1",
                table: "Order_Detail",
                column: "Product_ID1");

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Detail_Order_Order_ID1",
                table: "Order_Detail",
                column: "Order_ID1",
                principalTable: "Order",
                principalColumn: "Order_ID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Order_Detail_Product_Product_ID1",
                table: "Order_Detail",
                column: "Product_ID1",
                principalTable: "Product",
                principalColumn: "Product_ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
