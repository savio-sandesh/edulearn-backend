using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.Progress.API.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressPercentToLessonProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ProgressPercent",
                table: "LessonProgress",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "LessonProgress");
        }
    }
}
