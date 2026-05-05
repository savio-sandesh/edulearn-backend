using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.Course.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAverageRatingToCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageRating",
                table: "Courses",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "Courses");
        }
    }
}
