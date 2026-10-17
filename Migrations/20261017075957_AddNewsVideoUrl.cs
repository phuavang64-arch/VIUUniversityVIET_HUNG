using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIUUniversity.Migrations
{
    /// <summary>
    /// Thêm cột <c>News.VideoUrl</c> để bài viết kèm video nhúng.
    ///
    /// Tin tức của nhà trường rất nhiều là video (thông báo, hội thảo, hoạt động) chứ
    /// không phải bài viết dạng chữ. Trước đây quản trị viên chỉ có thể dán link vào ô
    /// nội dung — và <see cref="Services.ContentSanitizer"/> sẽ xoá nó vì không thuộc
    /// thẻ được phép. Cột riêng cho phép hiển thị video ở cả danh sách lẫn trang chi
    /// tiết, không lẫn vào nội dung chữ.
    /// </summary>
    public partial class AddNewsVideoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "News",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "News");
        }
    }
}