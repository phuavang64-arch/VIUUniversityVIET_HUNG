using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VIUUniversity.Migrations
{
    /// <inheritdoc />
    public partial class AddMajorDurationDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DurationDescription",
                table: "Majors",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationDescription",
                table: "Majors");
        }
    }
}
