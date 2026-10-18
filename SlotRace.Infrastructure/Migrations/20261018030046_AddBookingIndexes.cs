using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlotRace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Slots_StudentId",
                table: "Slots");

            migrationBuilder.DropIndex(
                name: "IX_Slots_TeacherId",
                table: "Slots");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_StudentId",
                table: "Slots",
                column: "StudentId",
                unique: true,
                filter: "\"StudentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_TeacherId_StartTimeUtc",
                table: "Slots",
                columns: new[] { "TeacherId", "StartTimeUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Slots_StudentId",
                table: "Slots");

            migrationBuilder.DropIndex(
                name: "IX_Slots_TeacherId_StartTimeUtc",
                table: "Slots");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_StudentId",
                table: "Slots",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Slots_TeacherId",
                table: "Slots",
                column: "TeacherId");
        }
    }
}
