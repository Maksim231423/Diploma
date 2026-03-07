using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningObjectivesToLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LearningObjectives",
                table: "Lessons",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LearningObjectives",
                table: "Lessons");
        }
    }
}
