using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Migrations
{
    /// <inheritdoc />
    public partial class Phase3a_ListingStatusAndImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the new status column with its default. Existing rows pick up
            //    "Active" temporarily; the backfill in step 2 overwrites them.
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "CarListings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            // 2. Backfill from the boolean while both columns still exist.
            migrationBuilder.Sql(
                "UPDATE CarListings SET status = CASE WHEN available = 1 THEN 'Active' ELSE 'Sold' END;");

            // 3. Redefine the view to filter on status so it stops depending on
            //    the boolean column before we drop it.
            migrationBuilder.Sql(@"
CREATE OR ALTER VIEW vw_AvailableCarDetails AS
SELECT
    C.id AS CarId, C.scraped_id, C.make, C.model, C.trim, C.year, C.kilometers,
    C.body_type, C.seats, C.fuel_type, C.transmission, C.engine_size,
    C.exterior_color, C.interior_color, C.regional_specs, C.price,
    C.interior_options, C.exterior_options, C.technology_options, C.images,
    CL.id AS ListingId, CL.listing_price, CL.payment_method_allowed,
    S.id AS SellerId, S.name AS SellerName, S.city, S.neighborhood,
    IR.body_condition, IR.paint_status, IR.description_score,
    ISNULL(IR.calculated_trust_score, 3.0) AS TrustScore
FROM Cars C
INNER JOIN CarListings CL ON C.id = CL.car_id
INNER JOIN Sellers S ON CL.seller_id = S.id
LEFT JOIN InspectionReports IR ON C.id = IR.car_id
WHERE CL.status = 'Active';");

            // 4. Drop the legacy index + column.
            migrationBuilder.DropIndex(
                name: "IX_CarListings_Availability",
                table: "CarListings");

            migrationBuilder.DropColumn(
                name: "available",
                table: "CarListings");

            // 5. CarImages — new table for FR-3.4 image storage.
            migrationBuilder.CreateTable(
                name: "CarImages",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    car_id = table.Column<int>(type: "int", nullable: false),
                    url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_primary = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarImages", x => x.id);
                    table.ForeignKey(
                        name: "FK_CarImages_Cars_car_id",
                        column: x => x.car_id,
                        principalTable: "Cars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 6. New indexes (NFR-Sc2).
            migrationBuilder.CreateIndex(
                name: "IX_Cars_Make",
                table: "Cars",
                column: "make");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Model",
                table: "Cars",
                column: "model");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Price",
                table: "Cars",
                column: "price");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Type",
                table: "Cars",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_Status",
                table: "CarListings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_CarImages_car_id",
                table: "CarImages",
                column: "car_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarImages");

            migrationBuilder.DropIndex(
                name: "IX_Cars_Make",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_Model",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_Price",
                table: "Cars");

            migrationBuilder.DropIndex(
                name: "IX_Cars_Type",
                table: "Cars");

            // 1. Re-introduce the boolean column with its old default.
            migrationBuilder.AddColumn<bool>(
                name: "available",
                table: "CarListings",
                type: "bit",
                nullable: true,
                defaultValue: true);

            // 2. Backfill from status while both columns still exist.
            migrationBuilder.Sql(
                "UPDATE CarListings SET available = CASE WHEN status = 'Active' THEN 1 ELSE 0 END;");

            // 3. Restore the view to its previous (boolean-based) definition.
            migrationBuilder.Sql(@"
CREATE OR ALTER VIEW vw_AvailableCarDetails AS
SELECT
    C.id AS CarId, C.scraped_id, C.make, C.model, C.trim, C.year, C.kilometers,
    C.body_type, C.seats, C.fuel_type, C.transmission, C.engine_size,
    C.exterior_color, C.interior_color, C.regional_specs, C.price,
    C.interior_options, C.exterior_options, C.technology_options, C.images,
    CL.id AS ListingId, CL.listing_price, CL.payment_method_allowed,
    S.id AS SellerId, S.name AS SellerName, S.city, S.neighborhood,
    IR.body_condition, IR.paint_status, IR.description_score,
    ISNULL(IR.calculated_trust_score, 3.0) AS TrustScore
FROM Cars C
INNER JOIN CarListings CL ON C.id = CL.car_id
INNER JOIN Sellers S ON CL.seller_id = S.id
LEFT JOIN InspectionReports IR ON C.id = IR.car_id
WHERE CL.available = 1;");

            migrationBuilder.DropIndex(
                name: "IX_CarListings_Status",
                table: "CarListings");

            migrationBuilder.DropColumn(
                name: "status",
                table: "CarListings");

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_Availability",
                table: "CarListings",
                column: "available");
        }
    }
}
