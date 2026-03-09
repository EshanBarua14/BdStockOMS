using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Day31_TradeAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeAlerts_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TradeAlerts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TradeAlerts_Users_InvestorId",
                        column: x => x.InvestorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TradeAlerts_BrokerageHouseId",
                table: "TradeAlerts",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeAlerts_InvestorId",
                table: "TradeAlerts",
                column: "InvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeAlerts_OrderId",
                table: "TradeAlerts",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeAlerts");
        }
    }
}
