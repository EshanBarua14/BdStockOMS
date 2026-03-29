using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Day89_CorporateActionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CorporateActionLedgers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CorporateActionId = table.Column<int>(type: "int", nullable: false),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    HoldingQtyAtRecord = table.Column<int>(type: "int", nullable: false),
                    ActionValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SharesAwarded = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorporateActionLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorporateActionLedgers_CorporateActions_CorporateActionId",
                        column: x => x.CorporateActionId,
                        principalTable: "CorporateActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CorporateActionLedgers_CorporateActionId",
                table: "CorporateActionLedgers",
                column: "CorporateActionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CorporateActionLedgers");
        }
    }
}
