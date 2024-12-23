using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libs.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scanners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScannerName = table.Column<string>(type: "TEXT", nullable: false),
                    Make = table.Column<string>(type: "TEXT", nullable: true),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    WatchedDir = table.Column<string>(type: "TEXT", nullable: false),
                    DestinationDir = table.Column<string>(type: "TEXT", nullable: false),
                    ArchiveDir = table.Column<string>(type: "TEXT", nullable: false),
                    ArtistName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scanners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ScannerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_Orders_Scanners_ScannerId",
                        column: x => x.ScannerId,
                        principalTable: "Scanners",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Rolls",
                columns: table => new
                {
                    RollId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RollNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    ImageCount = table.Column<int>(type: "INTEGER", nullable: true),
                    FilmType = table.Column<int>(type: "INTEGER", nullable: true),
                    RollNotes = table.Column<string>(type: "TEXT", nullable: true),
                    OrderId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rolls", x => x.RollId);
                    table.ForeignKey(
                        name: "FK_Rolls_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ScannerId",
                table: "Orders",
                column: "ScannerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rolls_OrderId",
                table: "Rolls",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rolls");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Scanners");
        }
    }
}
