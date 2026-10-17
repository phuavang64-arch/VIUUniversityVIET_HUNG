using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIUUniversity.Migrations
{
    /// <summary>
    /// Thêm cột <c>AdmissionMajors.Slug</c> và điền sẵn giá trị từ bảng <c>Majors</c>.
    ///
    /// Trang Tuyển sinh liệt kê ngành tuyển sinh dựa trên bảng AdmissionMajors, nhưng
    /// người đọc cần bấm vào từng ngành để xem chi tiết ở <c>/dao-tao/nganh/{slug}</c>.
    /// AdmissionMajor chỉ lưu MajorCode dạng chuỗi, không có khóa ngoại tới Majors, nên
    /// slug phải được sao chép vào đây. Nếu không, không có cách nào dựng được đường
    /// dẫn đúng mà phải tự sinh từ tên ngành — mà tên ngành ở hai bảng có thể lệch nhau.
    ///
    /// Bước điền dữ liệu chạy ngay sau khi thêm cột, nên mọi dòng hiện có đều có slug.
    /// Các dòng thêm sau đó do quản trị tạo thì controller sẽ tự điền.
    /// </summary>
    public partial class AddAdmissionMajorSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "AdmissionMajors",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            // Điền slug từ bảng Majors theo mã ngành. Lệnh này chỉ chạy đúng một lần
            // khi áp dụng migration; các dòng mới thêm sau đó do controller điền.
            migrationBuilder.Sql(@"
UPDATE ""AdmissionMajors""
   SET ""Slug"" = (SELECT ""Slug"" FROM ""Majors"" WHERE ""Majors"".""Code"" = ""AdmissionMajors"".""MajorCode"")
 WHERE ""Slug"" IS NULL
   AND EXISTS (SELECT 1 FROM ""Majors"" WHERE ""Majors"".""Code"" = ""AdmissionMajors"".""MajorCode"");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Slug",
                table: "AdmissionMajors");
        }
    }
}