using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowReturnRequestFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnRequestedAt",
                table: "BorrowRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnedToAdminUserId",
                table: "BorrowRecords",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_IsReturned_ReturnRequestedAt",
                table: "BorrowRecords",
                columns: new[] { "IsReturned", "ReturnRequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_ReturnedToAdminUserId",
                table: "BorrowRecords",
                column: "ReturnedToAdminUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowRecords_AspNetUsers_ReturnedToAdminUserId",
                table: "BorrowRecords",
                column: "ReturnedToAdminUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BorrowRecords_AspNetUsers_ReturnedToAdminUserId",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_IsReturned_ReturnRequestedAt",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_ReturnedToAdminUserId",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "ReturnRequestedAt",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "ReturnedToAdminUserId",
                table: "BorrowRecords");
        }
    }
}
