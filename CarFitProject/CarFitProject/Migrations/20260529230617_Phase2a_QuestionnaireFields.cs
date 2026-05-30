using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Migrations
{
    /// <inheritdoc />
    public partial class Phase2a_QuestionnaireFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "condition_pref",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "installment_months",
                table: "UserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "trip_type",
                table: "UserProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "condition_pref",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "installment_months",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "trip_type",
                table: "UserProfiles");
        }
    }
}
