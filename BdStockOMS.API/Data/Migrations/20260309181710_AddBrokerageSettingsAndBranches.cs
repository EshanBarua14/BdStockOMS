using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBrokerageSettingsAndBranches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BranchOffices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BranchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ManagerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchOffices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchOffices_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BrokerageSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    MaxSingleOrderValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MaxDailyTurnover = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MarginRatio = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MinCashBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsMarginTradingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsShortSellingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsSmsAlertEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailAlertEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsAutoSettlementEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsTwoFactorRequired = table.Column<bool>(type: "bit", nullable: false),
                    TradingStartMinutes = table.Column<int>(type: "int", nullable: false),
                    TradingEndMinutes = table.Column<int>(type: "int", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerageSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrokerageSettings_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchOffices_BrokerageHouseId_BranchCode",
                table: "BranchOffices",
                columns: new[] { "BrokerageHouseId", "BranchCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrokerageSettings_BrokerageHouseId",
                table: "BrokerageSettings",
                column: "BrokerageHouseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchOffices");

            migrationBuilder.DropTable(
                name: "BrokerageSettings");
        }
    }
}
