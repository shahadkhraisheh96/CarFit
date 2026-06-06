using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Migrations
{
    /// <inheritdoc />
    public partial class Phase6a_MechanicBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CarListingId",
                table: "InspectionBookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MechanicId",
                table: "InspectionBookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Mechanics",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mechanics", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionBookings_CarListingId",
                table: "InspectionBookings",
                column: "CarListingId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionBookings_MechanicId",
                table: "InspectionBookings",
                column: "MechanicId");

            migrationBuilder.CreateIndex(
                name: "IX_Mechanics_City",
                table: "Mechanics",
                column: "city");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionBookings_CarListings_CarListingId",
                table: "InspectionBookings",
                column: "CarListingId",
                principalTable: "CarListings",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionBookings_Mechanics_MechanicId",
                table: "InspectionBookings",
                column: "MechanicId",
                principalTable: "Mechanics",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionBookings_CarListings_CarListingId",
                table: "InspectionBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_InspectionBookings_Mechanics_MechanicId",
                table: "InspectionBookings");

            migrationBuilder.DropTable(
                name: "Mechanics");

            migrationBuilder.DropIndex(
                name: "IX_InspectionBookings_CarListingId",
                table: "InspectionBookings");

            migrationBuilder.DropIndex(
                name: "IX_InspectionBookings_MechanicId",
                table: "InspectionBookings");

            migrationBuilder.DropColumn(
                name: "CarListingId",
                table: "InspectionBookings");

            migrationBuilder.DropColumn(
                name: "MechanicId",
                table: "InspectionBookings");
        }
    }
}
