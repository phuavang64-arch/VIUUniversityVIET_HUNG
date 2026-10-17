using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIUUniversity.Migrations
{
    /// <inheritdoc />
    public partial class AddContentSectionsAndMajorSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "News",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentName",
                table: "News",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "News",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "News",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IssuedDate",
                table: "News",
                type: "TEXT",
                nullable: true);

            // defaultValue "TinTuc" (không phải "") để các bài đã có từ trước rơi
            // vào đúng chuyên mục ngay khi cột được thêm, không cần chạy backfill.
            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "News",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "TinTuc");

            migrationBuilder.AddColumn<string>(
                name: "SkillsDescription",
                table: "Majors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Majors",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            // Mọi truy vấn danh sách đều lọc theo Section; thiếu index thì cả bảng
            // phải quét mỗi lần tải trang.
            migrationBuilder.CreateIndex(
                name: "IX_News_Section",
                table: "News",
                column: "Section");

            // ---- Dữ liệu vốn có ----
            // Mã 7480103 "Kỹ thuật phần mềm" là mẫu tự tạo, không có trong danh sách
            // 21 ngành đại học chính quy K50 của Trường. Sửa tại chỗ thành 7480101
            // "Khoa học máy tính" (có thật) để không phải xoá dòng nào.
            //
            // SQLite không có tiền tố N'...' như SQL Server — chuỗi UTF-8 viết
            // thẳng trong dấu nháy đơn là đúng.
            migrationBuilder.Sql(@"
UPDATE ""Majors""
   SET ""Code"" = '7480101',
       ""Name"" = 'Khoa học máy tính',
       ""Slug"" = 'nganh-7480101-khoa-hoc-may-tinh'
 WHERE ""Code"" = '7480103';

UPDATE ""AdmissionMajors""
   SET ""MajorCode"" = '7480101'
 WHERE ""MajorCode"" = '7480103';

-- 7220201 ""Ngôn ngữ Anh"" không có trong danh sách K50 và không có ngành tương
-- đương để sửa tại chỗ. Dòng này CỐ TÌNH GIỮ LẠI (không xoá) vì có thể là ngành
-- ngoài hệ đại học chính quy K50; xem README mục 10. Cài mới thì SeedData chỉ tạo
-- đúng 21 ngành K50.
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE ""AdmissionMajors"" SET ""MajorCode"" = '7480103' WHERE ""MajorCode"" = '7480101';
UPDATE ""Majors"" SET ""Code"" = '7480103', ""Name"" = 'Kỹ thuật phần mềm', ""Slug"" = 'ky-thuat-phan-mem'
 WHERE ""Code"" = '7480101';
");

            migrationBuilder.DropIndex(
                name: "IX_News_Section",
                table: "News");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "News");

            migrationBuilder.DropColumn(
                name: "AttachmentName",
                table: "News");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "News");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "News");

            migrationBuilder.DropColumn(
                name: "IssuedDate",
                table: "News");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "News");

            migrationBuilder.DropColumn(
                name: "SkillsDescription",
                table: "Majors");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Majors");
        }
    }
}
