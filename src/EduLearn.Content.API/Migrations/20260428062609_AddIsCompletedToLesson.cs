using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduLearn.Content.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCompletedToLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Lessons",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Lessons");
        }
    }
}
