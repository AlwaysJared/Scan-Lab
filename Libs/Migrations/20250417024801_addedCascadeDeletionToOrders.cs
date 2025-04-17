using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Libs.Migrations
{
    /// <inheritdoc />
    public partial class addedCascadeDeletionToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rolls_Orders_OrderId",
                table: "Rolls");

            migrationBuilder.AddForeignKey(
                name: "FK_Rolls_Orders_OrderId",
                table: "Rolls",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rolls_Orders_OrderId",
                table: "Rolls");

            migrationBuilder.AddForeignKey(
                name: "FK_Rolls_Orders_OrderId",
                table: "Rolls",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");
        }
    }
}
