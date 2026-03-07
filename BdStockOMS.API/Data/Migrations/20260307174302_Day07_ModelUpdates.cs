using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Day07_ModelUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ApprovedAt",
                table: "Orders",
                newName: "CancelledAt");

            migrationBuilder.AddColumn<int>(
                name: "AccountType",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BONumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashBalance",
                table: "Users",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsBOAccountActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginLimit",
                table: "Users",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarginUsed",
                table: "Users",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BoardLotSize",
                table: "Stocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Stocks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CircuitBreakerHigh",
                table: "Stocks",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CircuitBreakerLow",
                table: "Stocks",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LimitPrice",
                table: "Orders",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderCategory",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlacedBy",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SettlementType",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    AverageBuyPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Portfolios_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Portfolios_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Portfolios_Users_InvestorId",
                        column: x => x.InvestorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "SuperAdmin");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "BrokerageHouse");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Admin");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "CCD");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "ITSupport");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "Trader");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[] { 7, "Investor" });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 418.55m, 342.45m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 57.53m, 47.07m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 236.50m, 193.50m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 35.86m, 29.34m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 108.35m, 88.65m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 1155.00m, 945.00m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 715.00m, 585.00m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 2310.00m, 1890.00m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 1320.00m, 1080.00m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow" },
                values: new object[] { 1, 0, 31.24m, 25.56m });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow", "CompanyName", "Exchange", "LastTradePrice", "TradingCode" },
                values: new object[] { 1, 4, 9.35m, 7.65m, "Aamra Networks Ltd", "DSE", 8.50m, "AAMRANET" });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow", "CompanyName", "LastTradePrice", "TradingCode" },
                values: new object[] { 1, 0, 418.00m, 342.00m, "Grameenphone Ltd", 380.00m, "GP" });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow", "CompanyName", "LastTradePrice", "TradingCode" },
                values: new object[] { 1, 0, 57.31m, 46.89m, "BRAC Bank Ltd", 52.10m, "BRACBANK" });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow", "CompanyName", "LastTradePrice", "TradingCode" },
                values: new object[] { 1, 0, 235.95m, 193.05m, "Square Pharmaceuticals Ltd", 214.50m, "SQURPHARMA" });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "BoardLotSize", "Category", "CircuitBreakerHigh", "CircuitBreakerLow", "CompanyName", "LastTradePrice", "TradingCode" },
                values: new object[] { 1, 0, 35.64m, 29.16m, "Islami Bank Bangladesh Ltd", 32.40m, "ISLAMIBANK" });

            migrationBuilder.InsertData(
                table: "Stocks",
                columns: new[] { "Id", "BoardLotSize", "Category", "Change", "ChangePercent", "CircuitBreakerHigh", "CircuitBreakerLow", "ClosePrice", "CompanyName", "Exchange", "HighPrice", "IsActive", "LastTradePrice", "LastUpdatedAt", "LowPrice", "TradingCode", "ValueInMillionTaka", "Volume" },
                values: new object[] { 16, 1, 0, 0m, 0m, 108.02m, 88.38m, 0m, "Dutch Bangla Bank Ltd", "CSE", 0m, true, 98.20m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "DUTCHBANGL", 0m, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_BrokerageHouseId",
                table: "Portfolios",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_InvestorId_StockId",
                table: "Portfolios",
                columns: new[] { "InvestorId", "StockId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_StockId",
                table: "Portfolios",
                column: "StockId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Portfolios");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DropColumn(
                name: "AccountType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BONumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CashBalance",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsBOAccountActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MarginLimit",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MarginUsed",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BoardLotSize",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "CircuitBreakerHigh",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "CircuitBreakerLow",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "LimitPrice",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderCategory",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PlacedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SettlementType",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "CancelledAt",
                table: "Orders",
                newName: "ApprovedAt");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "BrokerageHouse");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Admin");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "CCD");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "ITSupport");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "Name",
                value: "Trader");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "Investor");

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CompanyName", "Exchange", "LastTradePrice", "TradingCode" },
                values: new object[] { "Grameenphone Ltd", "CSE", 380.00m, "GP" });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CompanyName", "LastTradePrice", "TradingCode" },
                values: new object[] { "BRAC Bank Ltd", 52.10m, "BRACBANK" });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CompanyName", "LastTradePrice", "TradingCode" },
                values: new object[] { "Square Pharmaceuticals Ltd", 214.50m, "SQURPHARMA" });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CompanyName", "LastTradePrice", "TradingCode" },
                values: new object[] { "Islami Bank Bangladesh Ltd", 32.40m, "ISLAMIBANK" });

            migrationBuilder.UpdateData(
                table: "Stocks",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CompanyName", "LastTradePrice", "TradingCode" },
                values: new object[] { "Dutch Bangla Bank Ltd", 98.20m, "DUTCHBANGL" });
        }
    }
}
