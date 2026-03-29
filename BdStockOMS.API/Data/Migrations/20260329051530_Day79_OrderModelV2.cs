using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Day79_OrderModelV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AggressorIndicator",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Board",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClOrdID",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayQty",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExchangeId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExecInstruction",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExecutedQuantity",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "FillProbability",
                table: "Orders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossTradeAmt",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MinQty",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrigClOrdID",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlDate",
                table: "Orders",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SlippagePercent",
                table: "Orders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TimeInForce",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TrdMatchID",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Orders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    Pinned = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Scopes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "BackupHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Duration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    S3Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeeStructures",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BrokeragePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SecdFeePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CdblFeePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VatPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AitPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinBrokerage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApplyToCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeStructures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpWhitelist",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Ip = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpWhitelist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemRoles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "BackupHistory");

            migrationBuilder.DropTable(
                name: "FeeStructures");

            migrationBuilder.DropTable(
                name: "IpWhitelist");

            migrationBuilder.DropTable(
                name: "SystemRoles");

            migrationBuilder.DropColumn(
                name: "AggressorIndicator",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Board",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClOrdID",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DisplayQty",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExchangeId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExecInstruction",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExecutedQuantity",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FillProbability",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GrossTradeAmt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MinQty",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrigClOrdID",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SettlDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SlippagePercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TimeInForce",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TrdMatchID",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Orders");
        }
    }
}
