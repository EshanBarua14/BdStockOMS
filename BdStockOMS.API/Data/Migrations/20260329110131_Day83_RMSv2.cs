using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Day83_RMSv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EDRSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    TotalEquity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDebt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EDRRatio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarginUsed = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarginLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarginUtilPct = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EDRSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EDRSnapshots_Users_InvestorId",
                        column: x => x.InvestorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RMSLimitsV2",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    LimitType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    LimitValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WarnAt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActionOnBreach = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RMSLimitsV2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RMSLimitsV2_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EDRSnapshots_InvestorId",
                table: "EDRSnapshots",
                column: "InvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_RMSLimitsV2_BrokerageHouseId",
                table: "RMSLimitsV2",
                column: "BrokerageHouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EDRSnapshots");

            migrationBuilder.DropTable(
                name: "RMSLimitsV2");
        }
    }
}
