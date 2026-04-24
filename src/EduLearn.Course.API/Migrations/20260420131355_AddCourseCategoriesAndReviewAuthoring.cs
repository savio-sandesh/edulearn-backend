using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EduLearn.Course.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseCategoriesAndReviewAuthoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_CourseId",
                table: "Reviews");

            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CourseCategories",
                columns: table => new
                {
                    CourseCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseCategories", x => x.CourseCategoryId);
                });

            migrationBuilder.InsertData(
                table: "CourseCategories",
                columns: new[] { "CourseCategoryId", "Name" },
                values: new object[,]
                {
                    { 1, "Development" },
                    { 2, "Design" },
                    { 3, "Business" },
                    { 4, "Marketing" },
                    { 5, "IT & Software" },
                    { 6, "Personal Development" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CourseId_StudentId",
                table: "Reviews",
                columns: new[] { "CourseId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseCategories_Name",
                table: "CourseCategories",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseCategories");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CourseId_StudentId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Reviews");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CourseId",
                table: "Reviews",
                column: "CourseId");
        }
    }
}
