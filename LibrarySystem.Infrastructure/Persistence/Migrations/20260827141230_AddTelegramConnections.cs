using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: true),
                    TelegramUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConnectionCodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ConnectionCodeExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramConnections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_TelegramConnections_ChatId",
                table: "TelegramConnections",
                column: "ChatId",
                unique: true,
                filter: "[ChatId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_TelegramConnections_ConnectionCodeHash",
                table: "TelegramConnections",
                column: "ConnectionCodeHash",
                unique: true,
                filter: "[ConnectionCodeHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_TelegramConnections_UserId",
                table: "TelegramConnections",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramConnections");
        }
    }
}
