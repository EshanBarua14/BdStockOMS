// Data/AppDbContext.cs
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // ── DbSets = database tables ───────────────────
    public DbSet<Role> Roles { get; set; }
    public DbSet<BrokerageHouse> BrokerageHouses { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Portfolio> Portfolios { get; set; }
    public DbSet<SystemLog> SystemLogs { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<LoginHistory> LoginHistories { get; set; }
    public DbSet<PasswordHistory> PasswordHistories { get; set; }
    public DbSet<TwoFactorOtp> TwoFactorOtps { get; set; }
    public DbSet<TrustedDevice> TrustedDevices { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<CommissionRate> CommissionRates { get; set; }
    public DbSet<BrokerageCommissionRate> BrokerageCommissionRates { get; set; }
    public DbSet<InvestorCommissionRate> InvestorCommissionRates { get; set; }
    public DbSet<RMSLimit> RMSLimits { get; set; }
    public DbSet<SectorConfig> SectorConfigs { get; set; }
    public DbSet<CorporateAction> CorporateActions { get; set; }
    public DbSet<FundRequest> FundRequests { get; set; }
    public DbSet<MarketData> MarketData { get; set; }
    public DbSet<NewsItem> NewsItems { get; set; }
    public DbSet<Watchlist> Watchlists { get; set; }
    public DbSet<WatchlistItem> WatchlistItems { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }
    public DbSet<OrderAmendment> OrderAmendments { get; set; }
    public DbSet<TraderReassignment> TraderReassignments { get; set; }
    public DbSet<Trade> Trades { get; set; }
    public DbSet<OrderEvent> OrderEvents { get; set; }
    public DbSet<TradeAlert> TradeAlerts { get; set; }
    public DbSet<CommissionLedger> CommissionLedgers { get; set; }
    public DbSet<SettlementBatch> SettlementBatches { get; set; }
    public DbSet<SettlementItem> SettlementItems { get; set; }
    public DbSet<MarketDepth> MarketDepths { get; set; }
    public DbSet<KycDocument> KycDocuments { get; set; }
    public DbSet<KycApproval> KycApprovals { get; set; }
    public DbSet<PortfolioSnapshot> PortfolioSnapshots { get; set; }
    public DbSet<StockAnalytics> StockAnalytics { get; set; }
    public DbSet<FileImportBatch> FileImportBatches { get; set; }
    public DbSet<FileImportRow> FileImportRows { get; set; }
    public DbSet<BrokerageSettings> BrokerageSettings { get; set; }
    public DbSet<BranchOffice> BranchOffices { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    public DbSet<ContractNote> ContractNotes { get; set; }
    public DbSet<BosImportLog> BosImportLogs { get; set; }
    public DbSet<BrokerageConnection> BrokerageConnections { get; set; }
    public DbSet<TenantFeatureFlag> TenantFeatureFlags { get; set; }
    public DbSet<FIXMessageLog> FIXMessageLogs { get; set; }
    public DbSet<RMSLimitV2> RMSLimitsV2 { get; set; }
    public DbSet<EDRSnapshot> EDRSnapshots { get; set; }
    public DbSet<BOGroup> BOGroups { get; set; }
    public DbSet<BOGroupMember> BOGroupMembers { get; set; }
    public DbSet<Basket> Baskets { get; set; }
    public DbSet<BasketStock> BasketStocks { get; set; }
    public DbSet<BosImportSession> BosImportSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── ROLE ───────────────────────────────────
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(r => r.Name).IsUnique();
        });

        // ── USER ───────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.CashBalance).HasPrecision(18, 4);
            entity.Property(u => u.MarginLimit).HasPrecision(18, 4);
            entity.Property(u => u.MarginUsed).HasPrecision(18, 4);

            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.BrokerageHouse)
                  .WithMany(b => b.Users)
                  .HasForeignKey(u => u.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.AssignedTrader)
                  .WithMany()
                  .HasForeignKey(u => u.AssignedTraderId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        // ── STOCK ──────────────────────────────────
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasIndex(s => new { s.TradingCode, s.Exchange }).IsUnique();

            entity.Property(s => s.LastTradePrice).HasPrecision(18, 4);
            entity.Property(s => s.HighPrice).HasPrecision(18, 4);
            entity.Property(s => s.LowPrice).HasPrecision(18, 4);
            entity.Property(s => s.ClosePrice).HasPrecision(18, 4);
            entity.Property(s => s.Change).HasPrecision(18, 4);
            entity.Property(s => s.ChangePercent).HasPrecision(18, 4);
            entity.Property(s => s.ValueInMillionTaka).HasPrecision(18, 4);
            entity.Property(s => s.CircuitBreakerHigh).HasPrecision(18, 4);
            entity.Property(s => s.CircuitBreakerLow).HasPrecision(18, 4);
        });

        // ── ORDER ──────────────────────────────────
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.PriceAtOrder).HasPrecision(18, 4);
            entity.Property(o => o.ExecutionPrice).HasPrecision(18, 4);
            entity.Property(o => o.LimitPrice).HasPrecision(18, 4);

            entity.HasOne(o => o.Investor)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.InvestorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Trader)
                  .WithMany()
                  .HasForeignKey(o => o.TraderId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);

            entity.HasOne(o => o.Stock)
                  .WithMany(s => s.Orders)
                  .HasForeignKey(o => o.StockId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(o => o.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── PORTFOLIO ──────────────────────────────
        modelBuilder.Entity<Portfolio>(entity =>
        {
            // One investor can only have ONE record per stock
            entity.HasIndex(p => new { p.InvestorId, p.StockId }).IsUnique();

            entity.Property(p => p.AverageBuyPrice).HasPrecision(18, 4);

            entity.HasOne(p => p.Investor)
                  .WithMany(u => u.Portfolios)
                  .HasForeignKey(p => p.InvestorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Stock)
                  .WithMany(s => s.Portfolios)
                  .HasForeignKey(p => p.StockId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(p => p.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AUDITLOG ───────────────────────────────
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── REFRESH TOKEN ────────────────────────────
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(r => r.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── LOGIN HISTORY ──────────────────────────
        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.HasOne(l => l.User)
                  .WithMany(u => u.LoginHistories)
                  .HasForeignKey(l => l.UserId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
        });

        // ── PASSWORD HISTORY ───────────────────────────
        modelBuilder.Entity<PasswordHistory>(entity =>
        {
            entity.HasOne(p => p.User)
                  .WithMany(u => u.PasswordHistories)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TWO FACTOR OTP ─────────────────────────────
        modelBuilder.Entity<TwoFactorOtp>(entity =>
        {
            entity.HasOne(t => t.User)
                  .WithMany(u => u.TwoFactorOtps)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TRUSTED DEVICE ─────────────────────────────
        modelBuilder.Entity<TrustedDevice>(entity =>
        {
            entity.HasOne(t => t.User)
                  .WithMany(u => u.TrustedDevices)
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── USER SESSION ───────────────────────────────
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasOne(s => s.User)
                  .WithMany(u => u.UserSessions)
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── COMMISSION RATES ───────────────────────────
        modelBuilder.Entity<CommissionRate>(entity =>
        {
            entity.Property(c => c.BuyRate).HasPrecision(18, 4);
            entity.Property(c => c.SellRate).HasPrecision(18, 4);
            entity.Property(c => c.CDBLRate).HasPrecision(18, 4);
            entity.Property(c => c.DSEFeeRate).HasPrecision(18, 4);
        });

        modelBuilder.Entity<BrokerageCommissionRate>(entity =>
        {
            entity.Property(c => c.BuyRate).HasPrecision(18, 4);
            entity.Property(c => c.SellRate).HasPrecision(18, 4);
            entity.HasOne(c => c.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(c => c.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InvestorCommissionRate>(entity =>
        {
            entity.Property(c => c.BuyRate).HasPrecision(18, 4);
            entity.Property(c => c.SellRate).HasPrecision(18, 4);
            entity.HasOne(c => c.Investor)
                  .WithMany()
                  .HasForeignKey(c => c.InvestorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(c => c.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(c => c.ApprovedBy)
                  .WithMany()
                  .HasForeignKey(c => c.ApprovedByUserId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        // ── RMS LIMITS ─────────────────────────────────
        modelBuilder.Entity<RMSLimit>(entity =>
        {
            entity.Property(r => r.MaxOrderValue).HasPrecision(18, 4);
            entity.Property(r => r.MaxDailyValue).HasPrecision(18, 4);
            entity.Property(r => r.MaxExposure).HasPrecision(18, 4);
            entity.Property(r => r.ConcentrationPct).HasPrecision(18, 4);
            entity.HasOne(r => r.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(r => r.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SECTOR CONFIG ──────────────────────────────
        modelBuilder.Entity<SectorConfig>(entity =>
        {
            entity.Property(s => s.MaxConcentrationPct).HasPrecision(18, 4);
        });

        // ── CORPORATE ACTIONS ──────────────────────────
        modelBuilder.Entity<CorporateAction>(entity =>
        {
            entity.Property(c => c.Value).HasPrecision(18, 4);
            entity.HasOne(c => c.Stock)
                  .WithMany()
                  .HasForeignKey(c => c.StockId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── FUND REQUEST ───────────────────────────────
        modelBuilder.Entity<FundRequest>(entity =>
        {
            entity.Property(f => f.Amount).HasPrecision(18, 4);
            entity.HasOne(f => f.Investor)
                  .WithMany()
                  .HasForeignKey(f => f.InvestorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(f => f.Trader)
                  .WithMany()
                  .HasForeignKey(f => f.TraderId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
            entity.HasOne(f => f.CCDUser)
                  .WithMany()
                  .HasForeignKey(f => f.CCDUserId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
            entity.HasOne(f => f.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(f => f.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MARKET DATA ────────────────────────────────
        modelBuilder.Entity<MarketData>(entity =>
        {
            entity.Property(m => m.Open).HasPrecision(18, 4);
            entity.Property(m => m.High).HasPrecision(18, 4);
            entity.Property(m => m.Low).HasPrecision(18, 4);
            entity.Property(m => m.Close).HasPrecision(18, 4);
            entity.Property(m => m.ValueInMillionTaka).HasPrecision(18, 4);
            entity.HasOne(m => m.Stock)
                  .WithMany()
                  .HasForeignKey(m => m.StockId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(m => new { m.StockId, m.Date, m.Exchange }).IsUnique();
        });

        // ── NEWS ───────────────────────────────────────
        modelBuilder.Entity<NewsItem>(entity =>
        {
            entity.HasOne(n => n.RelatedStock)
                  .WithMany()
                  .HasForeignKey(n => n.RelatedStockId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        // ── WATCHLIST ──────────────────────────────────
        modelBuilder.Entity<Watchlist>(entity =>
        {
            entity.HasOne(w => w.User)
                  .WithMany()
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WatchlistItem>(entity =>
        {
            entity.HasIndex(w => new { w.WatchlistId, w.StockId }).IsUnique();
            entity.HasOne(w => w.Watchlist)
                  .WithMany(w => w.Items)
                  .HasForeignKey(w => w.WatchlistId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(w => w.Stock)
                  .WithMany()
                  .HasForeignKey(w => w.StockId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── NOTIFICATION ───────────────────────────────
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── SYSTEM SETTING ─────────────────────────────
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasIndex(s => s.Key).IsUnique();
            entity.HasOne(s => s.UpdatedBy)
                  .WithMany()
                  .HasForeignKey(s => s.UpdatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
        });

        // ── ORDER AMENDMENT ────────────────────────────
        modelBuilder.Entity<OrderAmendment>(entity =>
        {
            entity.Property(o => o.OldPrice).HasPrecision(18, 4);
            entity.Property(o => o.NewPrice).HasPrecision(18, 4);
            entity.HasOne(o => o.Order)
                  .WithMany()
                  .HasForeignKey(o => o.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(o => o.AmendedBy)
                  .WithMany()
                  .HasForeignKey(o => o.AmendedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── TRADER REASSIGNMENT ────────────────────────
        modelBuilder.Entity<TraderReassignment>(entity =>
        {
            entity.HasOne(t => t.Investor)
                  .WithMany()
                  .HasForeignKey(t => t.InvestorId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.OldTrader)
                  .WithMany()
                  .HasForeignKey(t => t.OldTraderId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
            entity.HasOne(t => t.NewTrader)
                  .WithMany()
                  .HasForeignKey(t => t.NewTraderId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.ReassignedBy)
                  .WithMany()
                  .HasForeignKey(t => t.ReassignedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(t => t.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SEED ROLES (7 roles now) ───────────────

        modelBuilder.Entity<Trade>(entity =>
        {
            entity.Property(t => t.Price).HasPrecision(18, 4);
            entity.Property(t => t.TotalValue).HasPrecision(18, 4);
        });

        modelBuilder.Entity<TradeAlert>(entity =>
        {
            entity.Property(t => t.ThresholdValue).HasPrecision(18, 4);
            entity.Property(t => t.ActualValue).HasPrecision(18, 4);
        });

        modelBuilder.Entity<MarketDepth>(entity =>
        {
            entity.HasIndex(d => d.StockId).IsUnique();
            entity.Property(d => d.Bid1Price).HasPrecision(18, 4);
            entity.Property(d => d.Bid2Price).HasPrecision(18, 4);
            entity.Property(d => d.Bid3Price).HasPrecision(18, 4);
            entity.Property(d => d.Bid4Price).HasPrecision(18, 4);
            entity.Property(d => d.Bid5Price).HasPrecision(18, 4);
            entity.Property(d => d.Ask1Price).HasPrecision(18, 4);
            entity.Property(d => d.Ask2Price).HasPrecision(18, 4);
            entity.Property(d => d.Ask3Price).HasPrecision(18, 4);
            entity.Property(d => d.Ask4Price).HasPrecision(18, 4);
            entity.Property(d => d.Ask5Price).HasPrecision(18, 4);
        });

        modelBuilder.Entity<SettlementBatch>(entity =>
        {
            entity.Property(s => s.TotalBuyValue).HasPrecision(18, 4);
            entity.Property(s => s.TotalSellValue).HasPrecision(18, 4);
            entity.Property(s => s.NetObligations).HasPrecision(18, 4);
        });

        modelBuilder.Entity<SettlementItem>(entity =>
        {
            entity.Property(s => s.Price).HasPrecision(18, 4);
            entity.Property(s => s.TradeValue).HasPrecision(18, 4);
            entity.Property(s => s.TotalCharges).HasPrecision(18, 4);
            entity.Property(s => s.NetAmount).HasPrecision(18, 4);
        });

        modelBuilder.Entity<CommissionLedger>(entity =>
        {
            entity.Property(c => c.TradeValue).HasPrecision(18, 4);
            entity.Property(c => c.BrokerCommission).HasPrecision(18, 4);
            entity.Property(c => c.CDBLCharge).HasPrecision(18, 4);
            entity.Property(c => c.ExchangeFee).HasPrecision(18, 4);
            entity.Property(c => c.TotalCharges).HasPrecision(18, 4);
            entity.Property(c => c.NetAmount).HasPrecision(18, 4);
            entity.Property(c => c.CommissionRate).HasPrecision(18, 6);
        });


        modelBuilder.Entity<BrokerageSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BrokerageHouseId).IsUnique();
            entity.HasOne(e => e.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(e => e.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BranchOffice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BranchCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.HasIndex(e => new { e.BrokerageHouseId, e.BranchCode }).IsUnique();
            entity.HasOne(e => e.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(e => e.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

                modelBuilder.Entity<FileImportBatch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.FileType).HasConversion<string>();
            entity.HasOne(e => e.UploadedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.UploadedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(e => e.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FileImportRow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RawData).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.ValidationError).HasMaxLength(1000);
            entity.Property(e => e.ParsedData).HasMaxLength(2000);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.FileImportBatch)
                  .WithMany(b => b.Rows)
                  .HasForeignKey(e => e.FileImportBatchId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

                modelBuilder.Entity<PortfolioSnapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.SnapshotDate });
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(e => e.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StockAnalytics>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StockId, e.Exchange }).IsUnique();
            entity.Property(e => e.Exchange).IsRequired().HasMaxLength(10);
            entity.HasOne(e => e.Stock)
                  .WithMany()
                  .HasForeignKey(e => e.StockId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

                modelBuilder.Entity<KycDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentNumber).IsRequired().HasMaxLength(200);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.BackFilePath).HasMaxLength(500);
            entity.Property(e => e.RejectionReason).HasMaxLength(1000);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.DocumentType).HasConversion<string>();
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KycApproval>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Remarks).HasMaxLength(1000);
            entity.Property(e => e.Action).HasConversion<string>();
            entity.HasOne(e => e.KycDocument)
                  .WithMany(d => d.KycApprovals)
                  .HasForeignKey(e => e.KycDocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ActorUser)
                  .WithMany()
                  .HasForeignKey(e => e.ActorUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.GrantedBy)
                  .WithMany()
                  .HasForeignKey(e => e.GrantedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.Permission }).IsUnique();
        });

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "SuperAdmin" },
            new Role { Id = 2, Name = "BrokerageHouse" },
            new Role { Id = 3, Name = "Admin" },
            new Role { Id = 4, Name = "CCD" },
            new Role { Id = 5, Name = "ITSupport" },
            new Role { Id = 6, Name = "Trader" },
            new Role { Id = 7, Name = "Investor" }
        );


        // ── SEED STOCKS ────────────────────────────
        modelBuilder.Entity<Stock>().HasData(
            // DSE Stocks
            new Stock { Id = 1,  TradingCode = "GP",         CompanyName = "Grameenphone Ltd",                    Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 380.50m, CircuitBreakerHigh = 418.55m, CircuitBreakerLow = 342.45m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 2,  TradingCode = "BRACBANK",   CompanyName = "BRAC Bank Ltd",                       Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 52.30m,  CircuitBreakerHigh = 57.53m,  CircuitBreakerLow = 47.07m,  IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 3,  TradingCode = "SQURPHARMA", CompanyName = "Square Pharmaceuticals Ltd",          Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 215.00m, CircuitBreakerHigh = 236.50m, CircuitBreakerLow = 193.50m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 4,  TradingCode = "ISLAMIBANK", CompanyName = "Islami Bank Bangladesh Ltd",          Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 32.60m,  CircuitBreakerHigh = 35.86m,  CircuitBreakerLow = 29.34m,  IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 5,  TradingCode = "DUTCHBANGL", CompanyName = "Dutch Bangla Bank Ltd",               Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 98.50m,  CircuitBreakerHigh = 108.35m, CircuitBreakerLow = 88.65m,  IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 6,  TradingCode = "RENATA",     CompanyName = "Renata Ltd",                          Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 1050.00m,CircuitBreakerHigh = 1155.00m,CircuitBreakerLow = 945.00m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 7,  TradingCode = "BATBC",      CompanyName = "British American Tobacco Bangladesh", Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 650.00m, CircuitBreakerHigh = 715.00m, CircuitBreakerLow = 585.00m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 8,  TradingCode = "MARICO",     CompanyName = "Marico Bangladesh Ltd",               Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 2100.00m,CircuitBreakerHigh = 2310.00m,CircuitBreakerLow = 1890.00m,IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 9,  TradingCode = "BERGERPBL",  CompanyName = "Berger Paints Bangladesh Ltd",        Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 1200.00m,CircuitBreakerHigh = 1320.00m,CircuitBreakerLow = 1080.00m,IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 10, TradingCode = "CITYBANK",   CompanyName = "The City Bank Ltd",                   Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 28.40m,  CircuitBreakerHigh = 31.24m,  CircuitBreakerLow = 25.56m,  IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            // DSE Z Category example
            new Stock { Id = 11, TradingCode = "AAMRANET",   CompanyName = "Aamra Networks Ltd",                  Exchange = "DSE", Category = StockCategory.Z, BoardLotSize = 1, LastTradePrice = 8.50m,   CircuitBreakerHigh = 9.35m,   CircuitBreakerLow = 7.65m,   IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            // CSE Stocks
            new Stock { Id = 12, TradingCode = "GP",         CompanyName = "Grameenphone Ltd",                    Exchange = "CSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 380.00m, CircuitBreakerHigh = 418.00m, CircuitBreakerLow = 342.00m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 13, TradingCode = "BRACBANK",   CompanyName = "BRAC Bank Ltd",                       Exchange = "CSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 52.10m,  CircuitBreakerHigh = 57.31m,  CircuitBreakerLow = 46.89m,  IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 14, TradingCode = "SQURPHARMA", CompanyName = "Square Pharmaceuticals Ltd",          Exchange = "CSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 214.50m, CircuitBreakerHigh = 235.95m, CircuitBreakerLow = 193.05m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 15, TradingCode = "ISLAMIBANK", CompanyName = "Islami Bank Bangladesh Ltd",          Exchange = "CSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 32.40m,  CircuitBreakerHigh = 35.64m,  CircuitBreakerLow = 29.16m,  IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 16, TradingCode = "DUTCHBANGL", CompanyName = "Dutch Bangla Bank Ltd",               Exchange = "CSE", Category = StockCategory.A, BoardLotSize = 1, LastTradePrice = 98.20m,  CircuitBreakerHigh = 108.02m, CircuitBreakerLow = 88.38m,  IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) }
        );
    }
    // ── Day 65: Admin Settings DbSets ──
    public DbSet<AppSetting>       AppSettings   => Set<AppSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FeeStructure>     FeeStructures => Set<FeeStructure>();
    public DbSet<SystemRole>       SystemRoles   => Set<SystemRole>();
    public DbSet<ApiKey>           ApiKeys       => Set<ApiKey>();
    public DbSet<Announcement>     Announcements => Set<Announcement>();
    public DbSet<BackupHistory>    BackupHistory => Set<BackupHistory>();
    public DbSet<IpWhitelistEntry> IpWhitelist   => Set<IpWhitelistEntry>();
    public DbSet<ComplianceReport> ComplianceReports => Set<ComplianceReport>();
}
