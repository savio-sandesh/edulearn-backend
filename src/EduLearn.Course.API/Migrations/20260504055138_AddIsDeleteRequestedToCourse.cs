using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.Course.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsDeleteRequestedToCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleteRequested",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleteRequested",
                table: "Courses");
        }
    }
}
