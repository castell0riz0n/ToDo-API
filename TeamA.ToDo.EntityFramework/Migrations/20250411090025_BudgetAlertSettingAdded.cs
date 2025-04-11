using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamA.ToDo.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class BudgetAlertSettingAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetAlertSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EnableAlerts = table.Column<bool>(type: "bit", nullable: false),
                    ThresholdPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SendMonthlySummary = table.Column<bool>(type: "bit", nullable: false),
                    LastAlertSent = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSummarySent = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetAlertSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BudgetAlertSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BudgetAlertSettings_UserId",
                table: "BudgetAlertSettings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BudgetAlertSettings");
        }
    }
}
