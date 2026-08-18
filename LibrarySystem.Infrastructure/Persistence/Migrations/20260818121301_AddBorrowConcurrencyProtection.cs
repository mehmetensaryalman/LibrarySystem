using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowConcurrencyProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_UserId",
                table: "BorrowRecords");

            migrationBuilder.CreateIndex(
                name: "UX_BorrowRecords_UserId_BookId_Active",
                table: "BorrowRecords",
                columns: new[] { "UserId", "BookId" },
                unique: true,
                filter: "[IsReturned] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_BorrowRecords_UserId_BookId_Active",
                table: "BorrowRecords");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_UserId",
                table: "BorrowRecords",
                column: "UserId");
        }
    }
}
