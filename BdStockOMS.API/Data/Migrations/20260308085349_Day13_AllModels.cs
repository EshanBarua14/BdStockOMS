using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Day13_AllModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrokerageCommissionRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    BuyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SellRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerageCommissionRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrokerageCommissionRates_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SellRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CDBLRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DSEFeeRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CorporateActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RecordDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CorporateActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CorporateActions_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FundRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    TraderId = table.Column<int>(type: "int", nullable: true),
                    CCDUserId = table.Column<int>(type: "int", nullable: true),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundRequests_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FundRequests_Users_CCDUserId",
                        column: x => x.CCDUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FundRequests_Users_InvestorId",
                        column: x => x.InvestorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FundRequests_Users_TraderId",
                        column: x => x.TraderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvestorCommissionRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    BuyRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SellRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestorCommissionRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestorCommissionRates_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvestorCommissionRates_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvestorCommissionRates_Users_InvestorId",
                        column: x => x.InvestorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarketData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    ValueInMillionTaka = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Trades = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketData_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    RelatedStockId = table.Column<int>(type: "int", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NewsItems_Stocks_RelatedStockId",
                        column: x => x.RelatedStockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderAmendments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    AmendedByUserId = table.Column<int>(type: "int", nullable: false),
                    OldQuantity = table.Column<int>(type: "int", nullable: true),
                    NewQuantity = table.Column<int>(type: "int", nullable: true),
                    OldPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    NewPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAmendments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAmendments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderAmendments_Users_AmendedByUserId",
                        column: x => x.AmendedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RMSLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    MaxOrderValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaxDailyValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaxExposure = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ConcentrationPct = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ActionOnBreach = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RMSLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RMSLimits_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SectorConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectorName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SectorCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxConcentrationPct = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TraderReassignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvestorId = table.Column<int>(type: "int", nullable: false),
                    OldTraderId = table.Column<int>(type: "int", nullable: true),
                    NewTraderId = table.Column<int>(type: "int", nullable: false),
                    ReassignedByUserId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraderReassignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraderReassignments_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraderReassignments_Users_InvestorId",
                        column: x => x.InvestorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraderReassignments_Users_NewTraderId",
                        column: x => x.NewTraderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraderReassignments_Users_OldTraderId",
                        column: x => x.OldTraderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TraderReassignments_Users_ReassignedByUserId",
                        column: x => x.ReassignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Watchlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Watchlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Watchlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WatchlistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WatchlistId = table.Column<int>(type: "int", nullable: false),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WatchlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WatchlistItems_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WatchlistItems_Watchlists_WatchlistId",
                        column: x => x.WatchlistId,
                        principalTable: "Watchlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerageCommissionRates_BrokerageHouseId",
                table: "BrokerageCommissionRates",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CorporateActions_StockId",
                table: "CorporateActions",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_FundRequests_BrokerageHouseId",
                table: "FundRequests",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_FundRequests_CCDUserId",
                table: "FundRequests",
                column: "CCDUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FundRequests_InvestorId",
                table: "FundRequests",
                column: "InvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_FundRequests_TraderId",
                table: "FundRequests",
                column: "TraderId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorCommissionRates_ApprovedByUserId",
                table: "InvestorCommissionRates",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorCommissionRates_BrokerageHouseId",
                table: "InvestorCommissionRates",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorCommissionRates_InvestorId",
                table: "InvestorCommissionRates",
                column: "InvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketData_StockId_Date_Exchange",
                table: "MarketData",
                columns: new[] { "StockId", "Date", "Exchange" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsItems_RelatedStockId",
                table: "NewsItems",
                column: "RelatedStockId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAmendments_AmendedByUserId",
                table: "OrderAmendments",
                column: "AmendedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAmendments_OrderId",
                table: "OrderAmendments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RMSLimits_BrokerageHouseId",
                table: "RMSLimits",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_UpdatedByUserId",
                table: "SystemSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TraderReassignments_BrokerageHouseId",
                table: "TraderReassignments",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TraderReassignments_InvestorId",
                table: "TraderReassignments",
                column: "InvestorId");

            migrationBuilder.CreateIndex(
                name: "IX_TraderReassignments_NewTraderId",
                table: "TraderReassignments",
                column: "NewTraderId");

            migrationBuilder.CreateIndex(
                name: "IX_TraderReassignments_OldTraderId",
                table: "TraderReassignments",
                column: "OldTraderId");

            migrationBuilder.CreateIndex(
                name: "IX_TraderReassignments_ReassignedByUserId",
                table: "TraderReassignments",
                column: "ReassignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_StockId",
                table: "WatchlistItems",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_WatchlistItems_WatchlistId_StockId",
                table: "WatchlistItems",
                columns: new[] { "WatchlistId", "StockId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Watchlists_UserId",
                table: "Watchlists",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrokerageCommissionRates");

            migrationBuilder.DropTable(
                name: "CommissionRates");

            migrationBuilder.DropTable(
                name: "CorporateActions");

            migrationBuilder.DropTable(
                name: "FundRequests");

            migrationBuilder.DropTable(
                name: "InvestorCommissionRates");

            migrationBuilder.DropTable(
                name: "MarketData");

            migrationBuilder.DropTable(
                name: "NewsItems");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OrderAmendments");

            migrationBuilder.DropTable(
                name: "RMSLimits");

            migrationBuilder.DropTable(
                name: "SectorConfigs");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TraderReassignments");

            migrationBuilder.DropTable(
                name: "WatchlistItems");

            migrationBuilder.DropTable(
                name: "Watchlists");
        }
    }
}
