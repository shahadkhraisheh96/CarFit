using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneIsPlaceholder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PhoneIsPlaceholder",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneIsPlaceholder",
                table: "AspNetUsers");
        }
    }
}
