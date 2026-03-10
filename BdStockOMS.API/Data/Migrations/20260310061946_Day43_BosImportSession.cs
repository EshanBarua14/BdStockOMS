using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Day43_BosImportSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BosImportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Md5Hash = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Md5Verified = table.Column<bool>(type: "bit", nullable: false),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    RecordsFailed = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BosImportLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BosImportSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    XmlFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CtrlFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpectedMd5 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActualMd5 = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Md5Verified = table.Column<bool>(type: "bit", nullable: false),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    ReconciledRecords = table.Column<int>(type: "int", nullable: false),
                    UnmatchedRecords = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BosImportSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BosImportSessions_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BosImportSessions_Users_ImportedByUserId",
                        column: x => x.ImportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BrokerageConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerageConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrokerageConnections_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractNoteNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    TraderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    InstrumentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InstrumentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Side = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ExecutedPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CdscFee = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    LevyCharge = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    VatOnCommission = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TradeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsVoid = table.Column<bool>(type: "bit", nullable: false),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractNotes_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractNotes_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BosImportSessions_BrokerageHouseId",
                table: "BosImportSessions",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_BosImportSessions_ImportedByUserId",
                table: "BosImportSessions",
                column: "ImportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BrokerageConnections_BrokerageHouseId",
                table: "BrokerageConnections",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractNotes_ClientId",
                table: "ContractNotes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractNotes_OrderId",
                table: "ContractNotes",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BosImportLogs");

            migrationBuilder.DropTable(
                name: "BosImportSessions");

            migrationBuilder.DropTable(
                name: "BrokerageConnections");

            migrationBuilder.DropTable(
                name: "ContractNotes");
        }
    }
}
