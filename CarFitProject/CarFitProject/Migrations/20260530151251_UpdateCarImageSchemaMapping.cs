using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCarImageSchemaMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarImages_Cars_car_id",
                table: "CarImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarImages",
                table: "CarImages");

            migrationBuilder.RenameTable(
                name: "CarImages",
                newName: "car_images");

            migrationBuilder.AddPrimaryKey(
                name: "PK_car_images",
                table: "car_images",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_car_images_Cars_car_id",
                table: "car_images",
                column: "car_id",
                principalTable: "Cars",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_car_images_Cars_car_id",
                table: "car_images");

            migrationBuilder.DropPrimaryKey(
                name: "PK_car_images",
                table: "car_images");

            migrationBuilder.RenameTable(
                name: "car_images",
                newName: "CarImages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarImages",
                table: "CarImages",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_CarImages_Cars_car_id",
                table: "CarImages",
                column: "car_id",
                principalTable: "Cars",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
