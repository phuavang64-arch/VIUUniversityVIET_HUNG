using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIUUniversity.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionMajorImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "AdmissionMajors",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE ""AdmissionMajors""
   SET ""ImageUrl"" = (SELECT ""ImageUrl"" FROM ""Majors"" WHERE ""Majors"".""Code"" = ""AdmissionMajors"".""MajorCode"")
 WHERE ""ImageUrl"" IS NULL
   AND EXISTS (SELECT 1 FROM ""Majors"" WHERE ""Majors"".""Code"" = ""AdmissionMajors"".""MajorCode"");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "AdmissionMajors");
        }
    }
}
