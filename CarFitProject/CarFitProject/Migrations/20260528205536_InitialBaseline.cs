using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cars",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    make = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    transmission = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    fuel_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    body_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    seats = table.Column<int>(type: "int", nullable: true),
                    fuel_efficiency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    images = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    scraped_id = table.Column<int>(type: "int", nullable: true),
                    trim = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    kilometers = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    engine_size = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    exterior_color = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    interior_color = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    regional_specs = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    interior_options = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    exterior_options = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    technology_options = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Cars__3213E83F43C12A3A", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "InspectionBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PackageType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreferredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VehicleNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InspectionTermsGlossary",
                columns: table => new
                {
                    term = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    severity_level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    explanation_ar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    explanation_en = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inspecti__E0F9670E11FE1C38", x => x.term);
                });

            migrationBuilder.CreateTable(
                name: "RecommendedCarMatches",
                columns: table => new
                {
                    car_id = table.Column<int>(type: "int", nullable: false),
                    make = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    listing_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    city = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    body_condition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description_score = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    trust_score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DynamicMatchScore = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Sellers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    city = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    neighborhood = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Sellers__3213E83F4F76A86D", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    profile_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    profile_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "My Fit Profile"),
                    age = table.Column<int>(type: "int", nullable: true),
                    marital_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    has_kids = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    kids_count = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    purpose = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    budget_min = table.Column<decimal>(type: "decimal(18,2)", nullable: true, defaultValue: 0m),
                    budget_max = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    payment_method = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    transmission_pref = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    size_pref = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserProf__AEBB701F5EA739D8", x => x.profile_id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__3213E83F2A7BC1EE", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "InspectionReports",
                columns: table => new
                {
                    car_id = table.Column<int>(type: "int", nullable: false),
                    center_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    inspection_date = table.Column<DateOnly>(type: "date", nullable: true),
                    chassis_1_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    chassis_2_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    chassis_3_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    chassis_4_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    body_condition = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    roof_condition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    engine_health_percent = table.Column<int>(type: "int", nullable: true),
                    engine_smoke = table.Column<bool>(type: "bit", nullable: true),
                    gearbox_status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    paint_filler_status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    carseer_attached = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    overall_score = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    description_score = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    calculated_trust_score = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    paint_status = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Inspecti__4C9A0DB3B4E9E2BC", x => x.car_id);
                    table.ForeignKey(
                        name: "FK__Inspectio__car_i__49C3F6B7",
                        column: x => x.car_id,
                        principalTable: "Cars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarListings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    car_id = table.Column<int>(type: "int", nullable: true),
                    seller_id = table.Column<int>(type: "int", nullable: true),
                    listing_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    available = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    installment_option = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    payment_method_allowed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CarListi__3213E83F8D2FA73D", x => x.id);
                    table.ForeignKey(
                        name: "FK__CarListin__car_i__44FF419A",
                        column: x => x.car_id,
                        principalTable: "Cars",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__CarListin__selle__45F365D3",
                        column: x => x.seller_id,
                        principalTable: "Sellers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "RecommendationLog",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    recommended_car_ids = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Recommen__3213E83F3C2D9495", x => x.id);
                    table.ForeignKey(
                        name: "FK__Recommend__user___5441852A",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "SavedResults",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    car_id = table.Column<int>(type: "int", nullable: false),
                    saved_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SavedRes__9D7797D4910E870F", x => new { x.user_id, x.car_id });
                    table.ForeignKey(
                        name: "FK__SavedResu__car_i__5070F446",
                        column: x => x.car_id,
                        principalTable: "Cars",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__SavedResu__user___4F7CD00D",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_Availability",
                table: "CarListings",
                column: "available");

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_car_id",
                table: "CarListings",
                column: "car_id");

            migrationBuilder.CreateIndex(
                name: "IX_CarListings_seller_id",
                table: "CarListings",
                column: "seller_id");

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Matching",
                table: "Cars",
                columns: new[] { "transmission", "body_type", "year" });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationLog_user_id",
                table: "RecommendationLog",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_SavedResults_car_id",
                table: "SavedResults",
                column: "car_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__AB6E61642DA1681D",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarListings");

            migrationBuilder.DropTable(
                name: "InspectionBookings");

            migrationBuilder.DropTable(
                name: "InspectionReports");

            migrationBuilder.DropTable(
                name: "InspectionTermsGlossary");

            migrationBuilder.DropTable(
                name: "RecommendationLog");

            migrationBuilder.DropTable(
                name: "RecommendedCarMatches");

            migrationBuilder.DropTable(
                name: "SavedResults");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "Sellers");

            migrationBuilder.DropTable(
                name: "Cars");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
