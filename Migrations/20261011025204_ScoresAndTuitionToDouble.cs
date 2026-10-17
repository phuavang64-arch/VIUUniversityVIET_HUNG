using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIUUniversity.Migrations
{
    /// <inheritdoc />
    public partial class ScoresAndTuitionToDouble : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Tuition",
                table: "Majors",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "real");

            migrationBuilder.AlterColumn<double>(
                name: "Score2025",
                table: "AdmissionMajors",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Score2024",
                table: "AdmissionMajors",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Score2023",
                table: "AdmissionMajors",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "real",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Tuition",
                table: "Majors",
                type: "real",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<decimal>(
                name: "Score2025",
                table: "AdmissionMajors",
                type: "real",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score2024",
                table: "AdmissionMajors",
                type: "real",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Score2023",
                table: "AdmissionMajors",
                type: "real",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);
        }
    }
}
