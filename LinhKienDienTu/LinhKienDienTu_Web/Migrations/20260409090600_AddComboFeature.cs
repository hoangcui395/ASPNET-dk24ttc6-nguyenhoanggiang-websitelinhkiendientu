using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinhKienDienTu_Web.Migrations
{
    /// <inheritdoc />
    public partial class AddComboFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCombo",
                table: "Product",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Combo_Detail",
                columns: table => new
                {
                    Combo_ID = table.Column<int>(type: "int", nullable: false),
                    Product_ID = table.Column<int>(type: "int", nullable: false),
                    So_Luong_Thanh_Phan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combo_Detail", x => new { x.Combo_ID, x.Product_ID });
                    table.ForeignKey(
                        name: "FK_Combo_Detail_Product_Combo_ID",
                        column: x => x.Combo_ID,
                        principalTable: "Product",
                        principalColumn: "Product_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Combo_Detail_Product_Product_ID",
                        column: x => x.Product_ID,
                        principalTable: "Product",
                        principalColumn: "Product_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Combo_Detail_Product_ID",
                table: "Combo_Detail",
                column: "Product_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Combo_Detail");

            migrationBuilder.DropColumn(
                name: "IsCombo",
                table: "Product");
        }
    }
}
