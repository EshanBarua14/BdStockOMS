using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioSnapshotsAndAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PortfolioSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalInvested = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CurrentValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnrealizedPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RealizedPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalPnL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RoiPercent = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CashBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalHoldings = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioSnapshots_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PortfolioSnapshots_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockAnalytics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Vwap = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    High52W = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Low52W = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Beta = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    AvgVolume30D = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockAnalytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockAnalytics_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSnapshots_BrokerageHouseId",
                table: "PortfolioSnapshots",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioSnapshots_UserId_SnapshotDate",
                table: "PortfolioSnapshots",
                columns: new[] { "UserId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StockAnalytics_StockId_Exchange",
                table: "StockAnalytics",
                columns: new[] { "StockId", "Exchange" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioSnapshots");

            migrationBuilder.DropTable(
                name: "StockAnalytics");
        }
    }
}
