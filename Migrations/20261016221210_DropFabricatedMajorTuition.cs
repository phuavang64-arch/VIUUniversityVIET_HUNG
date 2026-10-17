using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIUUniversity.Migrations
{
    /// <summary>
    /// Xoá các mức học phí do bản seed mẫu tự đặt.
    ///
    /// Trước đây mỗi ngành mang một con số "học phí/năm" cố định (13.000.000 –
    /// 15.000.000 đồng) do chính bản seed nhập, không có văn bản nào của Trường làm
    /// căn cứ. Số đó đang hiển thị công khai như thông tin chính thức của nhà trường,
    /// trong khi mức học phí thực tế đổi theo từng năm tuyển sinh.
    ///
    /// Học phí nay nằm ở chuyên mục /hoc-phi: mỗi văn bản/thông báo do Ban Tuyển
    /// sinh đăng kèm số ký hiệu, ngày ban hành, năm học áp dụng và tệp PDF. Cột
    /// Majors.Tuition giữ lại để tương thích, nhưng đặt 0 = "Chưa công bố".
    /// </summary>
    public partial class DropFabricatedMajorTuition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Majors\" SET \"Tuition\" = 0 WHERE \"Tuition\" > 0;");

            // Migration trước đã đổi AdmissionMajors.MajorCode 7480103 -> 7480101
            // nhưng bỏ sót tên, để lại "7480101 / Kỹ thuật phần mềm" trong bảng tra
            // cứu điểm chuẩn. Tên phải khớp mã.
            migrationBuilder.Sql(@"
UPDATE ""AdmissionMajors""
   SET ""MajorName"" = 'Khoa học máy tính'
 WHERE ""MajorCode"" = '7480101'
   AND ""MajorName"" = 'Kỹ thuật phần mềm';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không khôi phục: các giá trị cũ không có nguồn để đối chiếu.
        }
    }
}