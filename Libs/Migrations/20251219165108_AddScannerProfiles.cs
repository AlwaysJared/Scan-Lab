using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libs.Migrations
{
    /// <inheritdoc />
    public partial class AddScannerProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "Scanners",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScannerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileName = table.Column<string>(type: "text", nullable: false),
                    StrategyClassName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScannerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigKey = table.Column<string>(type: "text", nullable: false),
                    ConfigValue = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileConfigurations_ScannerProfiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "ScannerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Scanners_ProfileId",
                table: "Scanners",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileConfigurations_ProfileId",
                table: "ProfileConfigurations",
                column: "ProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scanners_ScannerProfiles_ProfileId",
                table: "Scanners",
                column: "ProfileId",
                principalTable: "ScannerProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scanners_ScannerProfiles_ProfileId",
                table: "Scanners");

            migrationBuilder.DropTable(
                name: "ProfileConfigurations");

            migrationBuilder.DropTable(
                name: "ScannerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Scanners_ProfileId",
                table: "Scanners");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "Scanners");
        }
    }
}
