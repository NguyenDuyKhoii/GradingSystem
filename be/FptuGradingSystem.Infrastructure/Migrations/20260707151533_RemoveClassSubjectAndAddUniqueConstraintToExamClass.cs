using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FptuGradingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClassSubjectAndAddUniqueConstraintToExamClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassSubjects");

            migrationBuilder.DropIndex(
                name: "IX_ExamClasses_ClassId",
                table: "ExamClasses");

            migrationBuilder.CreateIndex(
                name: "IX_ExamClasses_ClassId_SubjectId_Semester",
                table: "ExamClasses",
                columns: new[] { "ClassId", "SubjectId", "Semester" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExamClasses_ClassId_SubjectId_Semester",
                table: "ExamClasses");

            migrationBuilder.CreateTable(
                name: "ClassSubjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClassId = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    Semester = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassSubjects_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassSubjects_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamClasses_ClassId",
                table: "ExamClasses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjects_ClassId_SubjectId_Semester",
                table: "ClassSubjects",
                columns: new[] { "ClassId", "SubjectId", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjects_SubjectId",
                table: "ClassSubjects",
                column: "SubjectId");
        }
    }
}
