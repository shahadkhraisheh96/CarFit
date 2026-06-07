using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Migrations
{
    /// <inheritdoc />
    public partial class Phase10_SellerInspectionApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "CarListings",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Active");

            // Backfill existing listings onto the Phase 10 lifecycle states.
            // Done AFTER the column is widened so "PendingInspectionReview" (24 chars) fits.
            // Active/Available were the old public states -> Approved. Pending -> PendingInspectionReview.
            // Sold and any other values are left untouched.
            migrationBuilder.Sql(
                "UPDATE CarListings SET status = 'Approved' WHERE status IN ('Active', 'Available');");
            migrationBuilder.Sql(
                "UPDATE CarListings SET status = 'PendingInspectionReview' WHERE status = 'Pending';");

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "CarListings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SellerInspectionUploads",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    car_id = table.Column<int>(type: "int", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    stored_path = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    uploaded_by_user_id = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerInspectionUploads", x => x.id);
                    table.ForeignKey(
                        name: "FK_SellerInspectionUploads_Cars_car_id",
                        column: x => x.car_id,
                        principalTable: "Cars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerInspectionUploads_car_id",
                table: "SellerInspectionUploads",
                column: "car_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerInspectionUploads");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "CarListings");

            // Revert lifecycle states to their pre-Phase-10 values BEFORE shrinking the
            // column back to nvarchar(20) — otherwise "PendingInspectionReview" (24 chars)
            // would not fit. Rejected/Sold/Draft already fit within 20 chars.
            migrationBuilder.Sql(
                "UPDATE CarListings SET status = 'Active' WHERE status = 'Approved';");
            migrationBuilder.Sql(
                "UPDATE CarListings SET status = 'Pending' WHERE status = 'PendingInspectionReview';");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "CarListings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldDefaultValue: "Draft");
        }
    }
}
