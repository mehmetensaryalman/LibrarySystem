using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramBorrowReminderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDateReminderSentAt",
                table: "BorrowRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueReminderSentAt",
                table: "BorrowRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThreeDaysReminderSentAt",
                table: "BorrowRecords",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDateReminderSentAt",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "OverdueReminderSentAt",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "ThreeDaysReminderSentAt",
                table: "BorrowRecords");
        }
    }
}
