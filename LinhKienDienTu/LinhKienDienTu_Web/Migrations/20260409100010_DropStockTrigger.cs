using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinhKienDienTu_Web.Migrations
{
    /// <inheritdoc />
    public partial class DropStockTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DECLARE @TriggerName nvarchar(max);
                DECLARE tr_cursor CURSOR FOR 
                SELECT name FROM sys.triggers 
                WHERE parent_id = OBJECT_ID('Order_Detail');

                OPEN tr_cursor;
                FETCH NEXT FROM tr_cursor INTO @TriggerName;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    DECLARE @Definition nvarchar(max);
                    SELECT @Definition = OBJECT_DEFINITION(OBJECT_ID(@TriggerName));
                    
                    -- Nếu trigger có chứa thông báo lỗi về tồn kho, hãy xóa nó
                    IF @Definition LIKE N'%không đủ tồn kho%' OR @Definition LIKE N'%không đủ%'
                    BEGIN
                        EXEC('DROP TRIGGER [' + @TriggerName + ']');
                    END
                    
                    FETCH NEXT FROM tr_cursor INTO @TriggerName;
                END
                CLOSE tr_cursor;
                DEALLOCATE tr_cursor;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
