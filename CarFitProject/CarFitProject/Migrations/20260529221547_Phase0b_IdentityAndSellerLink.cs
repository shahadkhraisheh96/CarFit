using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarFitProject.Migrations
{
    /// <inheritdoc />
    public partial class Phase0b_IdentityAndSellerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Recommend__user___5441852A",
                table: "RecommendationLog");

            migrationBuilder.DropForeignKey(
                name: "FK__SavedResu__user___4F7CD00D",
                table: "SavedResults");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "Sellers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "identity_user_id",
                table: "Sellers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_approved",
                table: "Sellers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tier",
                table: "Sellers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // user_id is part of the composite PK; SQL Server won't ALTER COLUMN
            // while the constraint references it. Drop the PK, alter, re-add.
            migrationBuilder.DropPrimaryKey(
                name: "PK__SavedRes__9D7797D4910E870F",
                table: "SavedResults");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "SavedResults",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK__SavedRes__9D7797D4910E870F",
                table: "SavedResults",
                columns: new[] { "user_id", "car_id" });

            // IX_RecommendationLog_user_id sits on the column we're altering;
            // drop it first, change the column type, then recreate the index.
            migrationBuilder.DropIndex(
                name: "IX_RecommendationLog_user_id",
                table: "RecommendationLog");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "RecommendationLog",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationLog_user_id",
                table: "RecommendationLog",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_IdentityUserId",
                table: "Sellers",
                column: "identity_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sellers_IdentityUserId",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "email",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "identity_user_id",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "is_approved",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "tier",
                table: "Sellers");

            // Reverse the SavedResults PK rebuild around the column alter.
            migrationBuilder.DropPrimaryKey(
                name: "PK__SavedRes__9D7797D4910E870F",
                table: "SavedResults");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "SavedResults",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddPrimaryKey(
                name: "PK__SavedRes__9D7797D4910E870F",
                table: "SavedResults",
                columns: new[] { "user_id", "car_id" });

            // Reverse the RecommendationLog index drop/recreate around the column alter.
            migrationBuilder.DropIndex(
                name: "IX_RecommendationLog_user_id",
                table: "RecommendationLog");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "RecommendationLog",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationLog_user_id",
                table: "RecommendationLog",
                column: "user_id");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__3213E83F2A7BC1EE", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "UQ__Users__AB6E61642DA1681D",
                table: "Users",
                column: "email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK__Recommend__user___5441852A",
                table: "RecommendationLog",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__SavedResu__user___4F7CD00D",
                table: "SavedResults",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "id");
        }
    }
}
