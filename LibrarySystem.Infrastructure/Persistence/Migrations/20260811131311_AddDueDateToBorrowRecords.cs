using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDueDateToBorrowRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "BorrowRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
        UPDATE BorrowRecords
        SET DueDate = DATEADD(day, 7, BorrowDate)
        WHERE DueDate IS NULL;
        """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "BorrowRecords",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "BorrowRecords");
        }
    }
}
