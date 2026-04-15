using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinhKienDienTu_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCartTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    Category_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten_Danh_Muc = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.Category_ID);
                });

            migrationBuilder.CreateTable(
                name: "Promotion",
                columns: table => new
                {
                    Promotion_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten_Khuyen_Mai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phan_Tram_Giam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ngay_Bat_Dau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ngay_Ket_Thuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotion", x => x.Promotion_ID);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    User_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ho_Ten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mat_Khau = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    So_Dien_Thoai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Created_At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.User_ID);
                });

            migrationBuilder.CreateTable(
                name: "Sub_Category",
                columns: table => new
                {
                    SubCategory_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten_Danh_Muc_Con = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category_ID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sub_Category", x => x.SubCategory_ID);
                    table.ForeignKey(
                        name: "FK_Sub_Category_Category_Category_ID",
                        column: x => x.Category_ID,
                        principalTable: "Category",
                        principalColumn: "Category_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    Order_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User_ID = table.Column<int>(type: "int", nullable: false),
                    User_ID1 = table.Column<int>(type: "int", nullable: false),
                    Ngay_Dat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tong_Tien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Dia_Chi_Giao_Hang = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trang_Thai_Thanh_Toan = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.Order_ID);
                    table.ForeignKey(
                        name: "FK_Order_User_User_ID1",
                        column: x => x.User_ID1,
                        principalTable: "User",
                        principalColumn: "User_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    Product_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten_San_Pham = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gia_Goc = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Phan_Tram_Giam = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    So_Luong_Ton = table.Column<int>(type: "int", nullable: false),
                    Hinh_Anh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trang_Thai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubCategory_ID = table.Column<int>(type: "int", nullable: false),
                    Promotion_ID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Product_ID);
                    table.ForeignKey(
                        name: "FK_Product_Promotion_Promotion_ID",
                        column: x => x.Promotion_ID,
                        principalTable: "Promotion",
                        principalColumn: "Promotion_ID");
                    table.ForeignKey(
                        name: "FK_Product_Sub_Category_SubCategory_ID",
                        column: x => x.SubCategory_ID,
                        principalTable: "Sub_Category",
                        principalColumn: "SubCategory_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cart",
                columns: table => new
                {
                    Cart_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User_ID = table.Column<int>(type: "int", nullable: false),
                    Product_ID = table.Column<int>(type: "int", nullable: false),
                    So_Luong = table.Column<int>(type: "int", nullable: false),
                    Created_At = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Updated_At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cart", x => x.Cart_ID);
                    table.ForeignKey(
                        name: "FK_Cart_Product_Product_ID",
                        column: x => x.Product_ID,
                        principalTable: "Product",
                        principalColumn: "Product_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cart_User_User_ID",
                        column: x => x.User_ID,
                        principalTable: "User",
                        principalColumn: "User_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Order_Detail",
                columns: table => new
                {
                    OrderDetail_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Order_ID = table.Column<int>(type: "int", nullable: false),
                    Order_ID1 = table.Column<int>(type: "int", nullable: false),
                    Product_ID = table.Column<int>(type: "int", nullable: false),
                    Product_ID1 = table.Column<int>(type: "int", nullable: false),
                    So_Luong = table.Column<int>(type: "int", nullable: false),
                    Gia_Ban = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order_Detail", x => x.OrderDetail_ID);
                    table.ForeignKey(
                        name: "FK_Order_Detail_Order_Order_ID1",
                        column: x => x.Order_ID1,
                        principalTable: "Order",
                        principalColumn: "Order_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Order_Detail_Product_Product_ID1",
                        column: x => x.Product_ID1,
                        principalTable: "Product",
                        principalColumn: "Product_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cart_Product_ID",
                table: "Cart",
                column: "Product_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Cart_User_ID_Product_ID",
                table: "Cart",
                columns: new[] { "User_ID", "Product_ID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Order_User_ID1",
                table: "Order",
                column: "User_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Detail_Order_ID1",
                table: "Order_Detail",
                column: "Order_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Order_Detail_Product_ID1",
                table: "Order_Detail",
                column: "Product_ID1");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Promotion_ID",
                table: "Product",
                column: "Promotion_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Product_SubCategory_ID",
                table: "Product",
                column: "SubCategory_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Sub_Category_Category_ID",
                table: "Sub_Category",
                column: "Category_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cart");

            migrationBuilder.DropTable(
                name: "Order_Detail");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Promotion");

            migrationBuilder.DropTable(
                name: "Sub_Category");

            migrationBuilder.DropTable(
                name: "Category");
        }
    }
}
