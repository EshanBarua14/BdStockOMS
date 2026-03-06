using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrokerageHouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerageHouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TradingCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LastTradePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    HighPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LowPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ClosePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ChangePercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    ValueInMillionTaka = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    AssignedTraderId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_AssignedTraderId",
                        column: x => x.AssignedTraderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    TraderId = table.Column<int>(type: "int", nullable: true),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    OrderType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    PriceAtOrder = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ExecutionPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_InvestorId",
                        column: x => x.InvestorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_TraderId",
                        column: x => x.TraderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "BrokerageHouse" },
                    { 2, "Admin" },
                    { 3, "CCD" },
                    { 4, "ITSupport" },
                    { 5, "Trader" },
                    { 6, "Investor" }
                });

            migrationBuilder.InsertData(
                table: "Stocks",
                columns: new[] { "Id", "Change", "ChangePercent", "ClosePrice", "CompanyName", "Exchange", "HighPrice", "IsActive", "LastTradePrice", "LastUpdatedAt", "LowPrice", "TradingCode", "ValueInMillionTaka", "Volume" },
                values: new object[,]
                {
                    { 1, 0m, 0m, 0m, "Grameenphone Ltd", "DSE", 0m, true, 380.50m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "GP", 0m, 0L },
                    { 2, 0m, 0m, 0m, "BRAC Bank Ltd", "DSE", 0m, true, 52.30m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "BRACBANK", 0m, 0L },
                    { 3, 0m, 0m, 0m, "Square Pharmaceuticals Ltd", "DSE", 0m, true, 215.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "SQURPHARMA", 0m, 0L },
                    { 4, 0m, 0m, 0m, "Islami Bank Bangladesh Ltd", "DSE", 0m, true, 32.60m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "ISLAMIBANK", 0m, 0L },
                    { 5, 0m, 0m, 0m, "Dutch Bangla Bank Ltd", "DSE", 0m, true, 98.50m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "DUTCHBANGL", 0m, 0L },
                    { 6, 0m, 0m, 0m, "Renata Ltd", "DSE", 0m, true, 1050.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "RENATA", 0m, 0L },
                    { 7, 0m, 0m, 0m, "British American Tobacco Bangladesh", "DSE", 0m, true, 650.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "BATBC", 0m, 0L },
                    { 8, 0m, 0m, 0m, "Marico Bangladesh Ltd", "DSE", 0m, true, 2100.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "MARICO", 0m, 0L },
                    { 9, 0m, 0m, 0m, "Berger Paints Bangladesh Ltd", "DSE", 0m, true, 1200.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "BERGERPBL", 0m, 0L },
                    { 10, 0m, 0m, 0m, "The City Bank Ltd", "DSE", 0m, true, 28.40m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "CITYBANK", 0m, 0L },
                    { 11, 0m, 0m, 0m, "Grameenphone Ltd", "CSE", 0m, true, 380.00m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "GP", 0m, 0L },
                    { 12, 0m, 0m, 0m, "BRAC Bank Ltd", "CSE", 0m, true, 52.10m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "BRACBANK", 0m, 0L },
                    { 13, 0m, 0m, 0m, "Square Pharmaceuticals Ltd", "CSE", 0m, true, 214.50m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "SQURPHARMA", 0m, 0L },
                    { 14, 0m, 0m, 0m, "Islami Bank Bangladesh Ltd", "CSE", 0m, true, 32.40m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "ISLAMIBANK", 0m, 0L },
                    { 15, 0m, 0m, 0m, "Dutch Bangla Bank Ltd", "CSE", 0m, true, 98.20m, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0m, "DUTCHBANGL", 0m, 0L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BrokerageHouseId",
                table: "Orders",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_InvestorId",
                table: "Orders",
                column: "InvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StockId",
                table: "Orders",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TraderId",
                table: "Orders",
                column: "TraderId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_TradingCode_Exchange",
                table: "Stocks",
                columns: new[] { "TradingCode", "Exchange" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_AssignedTraderId",
                table: "Users",
                column: "AssignedTraderId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_BrokerageHouseId",
                table: "Users",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "SystemLogs");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "BrokerageHouses");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
