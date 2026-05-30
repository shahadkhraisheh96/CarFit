using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailableCarDetailsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('vw_AvailableCarDetails', 'V') IS NOT NULL DROP VIEW vw_AvailableCarDetails;");
        }
    }
}
