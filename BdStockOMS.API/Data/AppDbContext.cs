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
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemLog> SystemLogs { get; set; }

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

        // ── SEED ROLES (7 roles now) ───────────────
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
}
