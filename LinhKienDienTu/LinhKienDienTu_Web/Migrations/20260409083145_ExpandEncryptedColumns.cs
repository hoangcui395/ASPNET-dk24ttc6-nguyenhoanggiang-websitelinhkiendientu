using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LinhKienDienTu_Web.Migrations
{
    /// <inheritdoc />
    public partial class ExpandEncryptedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. Xóa Index cụ thể nếu tồn tại
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_User_Phone' AND object_id = OBJECT_ID('[User]'))
                    DROP INDEX [IX_User_Phone] ON [User];

                -- 2. Xóa Unique Constraint cụ thể (tên từ lỗi bạn cung cấp)
                IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UQ__User__1191139F07B97DEB' AND parent_object_id = OBJECT_ID('[User]'))
                    ALTER TABLE [User] DROP CONSTRAINT [UQ__User__1191139F07B97DEB];

                -- 3. Xóa bất kỳ Default Constraint nào trên cột này
                DECLARE @DefaultConstraint nvarchar(200);
                SELECT @DefaultConstraint = d.name FROM sys.default_constraints d
                INNER JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                WHERE d.parent_object_id = OBJECT_ID('[User]') AND c.name = 'So_Dien_Thoai';
                IF @DefaultConstraint IS NOT NULL EXEC('ALTER TABLE [User] DROP CONSTRAINT [' + @DefaultConstraint + ']');

                -- 4. Xóa bất kỳ Unique/Index nào khác còn sót lại liên quan đến cột này
                DECLARE @sql nvarchar(max) = '';
                SELECT @sql += 'ALTER TABLE [User] DROP CONSTRAINT ' + QUOTENAME(name) + ';'
                FROM sys.objects WHERE type = 'UQ' AND parent_object_id = OBJECT_ID('[User]');
                IF @sql <> '' EXEC sp_executesql @sql;
            ");

            // Thực hiện thay đổi kiểu dữ liệu cột
            migrationBuilder.Sql("ALTER TABLE [User] ALTER COLUMN [So_Dien_Thoai] NVARCHAR(MAX) NOT NULL;");
            migrationBuilder.Sql("ALTER TABLE [Order] ALTER COLUMN [Dia_Chi_Giao_Hang] NVARCHAR(MAX) NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
