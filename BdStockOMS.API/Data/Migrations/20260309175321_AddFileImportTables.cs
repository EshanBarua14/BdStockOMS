using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFileImportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    BrokerageHouseId = table.Column<int>(type: "int", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    ValidRows = table.Column<int>(type: "int", nullable: false),
                    InvalidRows = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileImportBatches_BrokerageHouses_BrokerageHouseId",
                        column: x => x.BrokerageHouseId,
                        principalTable: "BrokerageHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileImportBatches_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FileImportRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileImportBatchId = table.Column<int>(type: "int", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    RawData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParsedData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileImportRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileImportRows_FileImportBatches_FileImportBatchId",
                        column: x => x.FileImportBatchId,
                        principalTable: "FileImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileImportBatches_BrokerageHouseId",
                table: "FileImportBatches",
                column: "BrokerageHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_FileImportBatches_UploadedByUserId",
                table: "FileImportBatches",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileImportRows_FileImportBatchId",
                table: "FileImportRows",
                column: "FileImportBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileImportRows");

            migrationBuilder.DropTable(
                name: "FileImportBatches");
        }
    }
}
