using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Data;

// DbContext = the brain of EF Core
// Represents our entire database in C#
// Inherits from DbContext (EF Core base class)
public class AppDbContext : DbContext
{
    // Constructor — receives options from Program.cs
    // options contains our connection string
    // base(options) passes it to EF Core's DbContext
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // ── DbSets = our database tables ──────────────
    // Each DbSet<T> represents one table
    // Property name = table name in SQL Server

    public DbSet<Role> Roles { get; set; }
    public DbSet<BrokerageHouse> BrokerageHouses { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemLog> SystemLogs { get; set; }

    // ── Fluent API Configuration ───────────────────
    // Called automatically by EF Core when building
    // the database model
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Always call base first
        base.OnModelCreating(modelBuilder);

        // ── ROLE CONFIGURATION ─────────────────────
        modelBuilder.Entity<Role>(entity =>
        {
            // Unique index on Name
            // No two roles can have same name
            entity.HasIndex(r => r.Name)
                  .IsUnique();
        });

        // ── USER CONFIGURATION ─────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            // Unique index on Email
            // No two users can have same email
            entity.HasIndex(u => u.Email)
                  .IsUnique();

            // User belongs to ONE Role
            // One Role has MANY Users
            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
            // Restrict = can't delete a Role if
            // users are still assigned to it

            // User belongs to ONE BrokerageHouse
            entity.HasOne(u => u.BrokerageHouse)
                  .WithMany(b => b.Users)
                  .HasForeignKey(u => u.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Self-referencing — Investor has a Trader
            entity.HasOne(u => u.AssignedTrader)
                  .WithMany()
                  .HasForeignKey(u => u.AssignedTraderId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);
            // IsRequired(false) = FK is nullable (optional)
        });

        // ── STOCK CONFIGURATION ────────────────────
        modelBuilder.Entity<Stock>(entity =>
        {
            // Unique combination of TradingCode + Exchange
            // GP on DSE and GP on CSE = different records
            entity.HasIndex(s => new { s.TradingCode, s.Exchange })
                  .IsUnique();

            // decimal precision — 18 digits, 4 decimal places
            // Handles prices like 380.5000 taka
            entity.Property(s => s.LastTradePrice)
                  .HasPrecision(18, 4);
            entity.Property(s => s.HighPrice)
                  .HasPrecision(18, 4);
            entity.Property(s => s.LowPrice)
                  .HasPrecision(18, 4);
            entity.Property(s => s.ClosePrice)
                  .HasPrecision(18, 4);
            entity.Property(s => s.Change)
                  .HasPrecision(18, 4);
            entity.Property(s => s.ChangePercent)
                  .HasPrecision(18, 4);
            entity.Property(s => s.ValueInMillionTaka)
                  .HasPrecision(18, 4);
        });

        // ── ORDER CONFIGURATION ────────────────────
        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.PriceAtOrder)
                  .HasPrecision(18, 4);
            entity.Property(o => o.ExecutionPrice)
                  .HasPrecision(18, 4);

            // Order placed BY an Investor
            entity.HasOne(o => o.Investor)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.InvestorId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Order executed BY a Trader (optional)
            entity.HasOne(o => o.Trader)
                  .WithMany()
                  .HasForeignKey(o => o.TraderId)
                  .OnDelete(DeleteBehavior.Restrict)
                  .IsRequired(false);

            // Order is FOR a Stock
            entity.HasOne(o => o.Stock)
                  .WithMany(s => s.Orders)
                  .HasForeignKey(o => o.StockId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Order belongs to a BrokerageHouse
            entity.HasOne(o => o.BrokerageHouse)
                  .WithMany()
                  .HasForeignKey(o => o.BrokerageHouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── AUDITLOG CONFIGURATION ─────────────────
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── SEED DATA ──────────────────────────────
        // Seed = initial data inserted when DB is created
        // Roles are always fixed — seed them here
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "BrokerageHouse" },
            new Role { Id = 2, Name = "Admin" },
            new Role { Id = 3, Name = "CCD" },
            new Role { Id = 4, Name = "ITSupport" },
            new Role { Id = 5, Name = "Trader" },
            new Role { Id = 6, Name = "Investor" }
        );

        // Seed DSE Stocks
        modelBuilder.Entity<Stock>().HasData(
            new Stock { Id = 1, TradingCode = "GP", CompanyName = "Grameenphone Ltd", Exchange = "DSE", LastTradePrice = 380.50m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 2, TradingCode = "BRACBANK", CompanyName = "BRAC Bank Ltd", Exchange = "DSE", LastTradePrice = 52.30m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 3, TradingCode = "SQURPHARMA", CompanyName = "Square Pharmaceuticals Ltd", Exchange = "DSE", LastTradePrice = 215.00m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 4, TradingCode = "ISLAMIBANK", CompanyName = "Islami Bank Bangladesh Ltd", Exchange = "DSE", LastTradePrice = 32.60m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 5, TradingCode = "DUTCHBANGL", CompanyName = "Dutch Bangla Bank Ltd", Exchange = "DSE", LastTradePrice = 98.50m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 6, TradingCode = "RENATA", CompanyName = "Renata Ltd", Exchange = "DSE", LastTradePrice = 1050.00m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 7, TradingCode = "BATBC", CompanyName = "British American Tobacco Bangladesh", Exchange = "DSE", LastTradePrice = 650.00m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 8, TradingCode = "MARICO", CompanyName = "Marico Bangladesh Ltd", Exchange = "DSE", LastTradePrice = 2100.00m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 9, TradingCode = "BERGERPBL", CompanyName = "Berger Paints Bangladesh Ltd", Exchange = "DSE", LastTradePrice = 1200.00m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 10, TradingCode = "CITYBANK", CompanyName = "The City Bank Ltd", Exchange = "DSE", LastTradePrice = 28.40m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            // CSE Stocks
            new Stock { Id = 11, TradingCode = "GP", CompanyName = "Grameenphone Ltd", Exchange = "CSE", LastTradePrice = 380.00m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 12, TradingCode = "BRACBANK", CompanyName = "BRAC Bank Ltd", Exchange = "CSE", LastTradePrice = 52.10m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 13, TradingCode = "SQURPHARMA", CompanyName = "Square Pharmaceuticals Ltd", Exchange = "CSE", LastTradePrice = 214.50m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 14, TradingCode = "ISLAMIBANK", CompanyName = "Islami Bank Bangladesh Ltd", Exchange = "CSE", LastTradePrice = 32.40m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) },
            new Stock { Id = 15, TradingCode = "DUTCHBANGL", CompanyName = "Dutch Bangla Bank Ltd", Exchange = "CSE", LastTradePrice = 98.20m, IsActive = true, LastUpdatedAt = new DateTime(2026, 1, 1) }
        );
    }
}