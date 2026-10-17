using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIUUniversity.Migrations
{
    /// <summary>
    /// Cho phép bài viết không gắn chuyên mục tin.
    ///
    /// Tin tức thì luôn có chuyên mục, nhưng các chuyên mục mới (Nghiên cứu khoa
    /// học, Hợp tác, Học phí) phần lớn nội dung không thuộc danh mục tin nào — ví
    /// dụ một văn bản học phí. Trước đây cột bắt buộc, nên quản trị viên hoặc phải
    /// chọn một danh mục không liên quan, hoặc để 0 — mà 0 vi phạm khoá ngoại và
    /// làm trang lỗi 500 ngay khi bấm Lưu.
    /// </summary>
    public partial class MakeNewsCategoryOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "News",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"News\" SET \"CategoryId\" = " +
                "(SELECT MIN(\"Id\") FROM \"NewsCategories\") WHERE \"CategoryId\" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "News",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}