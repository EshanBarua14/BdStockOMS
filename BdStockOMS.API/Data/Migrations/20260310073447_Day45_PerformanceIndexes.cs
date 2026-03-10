using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BdStockOMS.API.Data.Migrations
{
    /// <inheritdoc />
    public partial class Day45_PerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Orders — composite indexes for most frequent query patterns
            migrationBuilder.CreateIndex(
                name: "IX_Orders_InvestorId_Status",
                table: "Orders",
                columns: new[] { "InvestorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BrokerageHouseId_Status",
                table: "Orders",
                columns: new[] { "BrokerageHouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StockId_Status",
                table: "Orders",
                columns: new[] { "StockId", "Status" });

            // AuditLogs — compliance queries by user+date and entity
            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            // Notifications — unread per user (composite, different from IX_Notifications_UserId)
            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            // BosImportSessions — history per brokerage ordered by date
            migrationBuilder.CreateIndex(
                name: "IX_BosImportSessions_BrokerageHouseId_ImportedAt",
                table: "BosImportSessions",
                columns: new[] { "BrokerageHouseId", "ImportedAt" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex("IX_Orders_InvestorId_Status", "Orders");
            migrationBuilder.DropIndex("IX_Orders_BrokerageHouseId_Status", "Orders");
            migrationBuilder.DropIndex("IX_Orders_StockId_Status", "Orders");
            migrationBuilder.DropIndex("IX_AuditLogs_UserId_CreatedAt", "AuditLogs");
            migrationBuilder.DropIndex("IX_AuditLogs_EntityType_EntityId", "AuditLogs");
            migrationBuilder.DropIndex("IX_Notifications_UserId_IsRead", "Notifications");
            migrationBuilder.DropIndex("IX_BosImportSessions_BrokerageHouseId_ImportedAt", "BosImportSessions");
        }
    }
}
