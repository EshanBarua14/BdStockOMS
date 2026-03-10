using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Xunit;

namespace BdStockOMS.Tests.Unit
{
    public class ConcurrencyTests
    {
        private static string _sharedDbName = string.Empty;

        private AppDbContext CreateDb(string? name = null)
        {
            var dbName = name ?? Guid.NewGuid().ToString();
            _sharedDbName = dbName;
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        private AppDbContext OpenDb(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        private async Task<(BrokerageHouse, Role, User)> SeedBasicDataAsync(AppDbContext db)
        {
            var brokerage = new BrokerageHouse
            {
                Id = 1, Name = "Test Brokerage", LicenseNumber = "LIC001"
            };
            var role = new Role { Id = 1, Name = "Investor" };
            var user = new User
            {
                Id = 1, BrokerageHouseId = 1, RoleId = 1,
                FullName = "Test Investor", Email = "inv@test.com",
                PasswordHash = "hash"
            };
            db.BrokerageHouses.Add(brokerage);
            db.Roles.Add(role);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return (brokerage, role, user);
        }

        // ── Concurrent Order Creation ─────────────────────────────────────

        [Fact]
        public async Task ConcurrentOrderCreation_MultipleThreads_AllOrdersSaved()
        {
            var db = CreateDb();
            var dbName = _sharedDbName;
            await SeedBasicDataAsync(db);

            var stock = new Stock
            {
                Id = 1, TradingCode = "GP", CompanyName = "Grameenphone",
                LastTradePrice = 350m, IsActive = true
            };
            db.Stocks.Add(stock);
            await db.SaveChangesAsync();

            const int threadCount = 10;
            var tasks = new List<Task>();
            var successCount = 0;
            var lockObj = new object();

            for (int i = 0; i < threadCount; i++)
            {
                var orderId = i;
                tasks.Add(Task.Run(async () =>
                {
                    var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase(dbName)
                        .Options;
                    using var threadDb = new AppDbContext(options);

                    var order = new Order
                    {
                        InvestorId      = 1,
                        BrokerageHouseId = 1,
                        StockId         = 1,
                        OrderType       = OrderType.Buy,
                        Quantity        = 10,
                        PriceAtOrder    = 350m,
                        Status          = OrderStatus.Pending,
                    };
                    threadDb.Orders.Add(order);
                    await threadDb.SaveChangesAsync();

                    lock (lockObj) { successCount++; }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, successCount);
        }

        [Fact]
        public async Task ConcurrentOrderCreation_AllOrdersHaveUniqueIds()
        {
            var db = CreateDb();
            var dbName = _sharedDbName;
            await SeedBasicDataAsync(db);
            var stock = new Stock { Id = 1, TradingCode = "BRAC", CompanyName = "BRAC Bank", LastTradePrice = 50m, IsActive = true };
            db.Stocks.Add(stock);
            await db.SaveChangesAsync();

            const int threadCount = 20;
            var orderIds = new System.Collections.Concurrent.ConcurrentBag<int>();
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase(dbName)
                        .Options;
                    using var threadDb = new AppDbContext(options);
                    var order = new Order
                    {
                        InvestorId = 1, BrokerageHouseId = 1, StockId = 1,
                        OrderType = OrderType.Buy, Quantity = 5, PriceAtOrder = 50m,
                        Status = OrderStatus.Pending,
                        
                    };
                    threadDb.Orders.Add(order);
                    await threadDb.SaveChangesAsync();
                    orderIds.Add(order.Id);
                }));
            }

            await Task.WhenAll(tasks);
            var distinctIds = orderIds.Distinct().Count();
            Assert.Equal(threadCount, distinctIds);
        }

        // ── Concurrent Portfolio Updates ──────────────────────────────────

        [Fact]
        public async Task ConcurrentPortfolioReads_DoNotThrow()
        {
            var db = CreateDb();
            var dbName = _sharedDbName;
            await SeedBasicDataAsync(db);
            var stock = new Stock { Id = 1, TradingCode = "GP", CompanyName = "Grameenphone", LastTradePrice = 350m, IsActive = true };
            db.Stocks.Add(stock);
            db.Portfolios.Add(new Portfolio
            {
                InvestorId = 1, StockId = 1, BrokerageHouseId = 1,
                Quantity = 100, AverageBuyPrice = 300m
            });
            await db.SaveChangesAsync();

            const int threadCount = 15;
            var tasks = new List<Task>();
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseInMemoryDatabase(dbName)
                            .Options;
                        using var threadDb = new AppDbContext(options);
                        var portfolio = await threadDb.Portfolios
                            .Where(p => p.InvestorId == 1)
                            .ToListAsync();
                        Assert.NotEmpty(portfolio);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Empty(exceptions);
        }

        // ── Concurrent Audit Log Writes ───────────────────────────────────

        [Fact]
        public async Task ConcurrentAuditLogWrites_AllEntriesSaved()
        {
            var db = CreateDb();
            var dbName = _sharedDbName;
            await SeedBasicDataAsync(db);

            const int threadCount = 25;
            var tasks = new List<Task>();
            var successCount = 0;
            var lockObj = new object();

            for (int i = 0; i < threadCount; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase(dbName)
                        .Options;
                    using var threadDb = new AppDbContext(options);
                    threadDb.AuditLogs.Add(new AuditLog
                    {
                        UserId      = 1,
                        Action      = $"ConcurrentAction_{index}",
                        EntityType  = "Order",
                        EntityId    = index,
                        CreatedAt   = DateTime.UtcNow,
                        IpAddress   = "127.0.0.1"
                    });
                    await threadDb.SaveChangesAsync();
                    lock (lockObj) { successCount++; }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, successCount);
        }

        // ── RMS Concurrent Balance Checks ─────────────────────────────────

        [Fact]
        public async Task ConcurrentRmsChecks_ReadsSameBalanceConsistently()
        {
            var db = CreateDb();
            var dbName = _sharedDbName;
            await SeedBasicDataAsync(db);

            db.RMSLimits.Add(new RMSLimit
            {
                Level            = RMSLevel.Investor,
                EntityId         = 1,
                EntityType       = "User",
                BrokerageHouseId = 1,
                MaxOrderValue    = 50000m,
                MaxDailyValue    = 200000m,
                MaxExposure      = 300000m,
                IsActive         = true
            });
            await db.SaveChangesAsync();

            const int threadCount = 20;
            var balances = new System.Collections.Concurrent.ConcurrentBag<decimal>();
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase(dbName)
                        .Options;
                    using var threadDb = new AppDbContext(options);
                    var rms = await threadDb.RMSLimits
                        .FirstOrDefaultAsync(r => r.EntityId == 1 && r.EntityType == "User");
                    if (rms != null)
                        balances.Add(rms.MaxOrderValue);
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, balances.Count);
            Assert.All(balances, b => Assert.Equal(50000m, b));
        }

        // ── Concurrent BOS Session Writes ─────────────────────────────────

        [Fact]
        public async Task ConcurrentBosSessionWrites_AllSessionsSaved()
        {
            var db = CreateDb();
            var dbName = _sharedDbName;
            await SeedBasicDataAsync(db);

            const int threadCount = 10;
            var tasks = new List<Task>();
            var successCount = 0;
            var lockObj = new object();

            for (int i = 0; i < threadCount; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    var options = new DbContextOptionsBuilder<AppDbContext>()
                        .UseInMemoryDatabase(dbName)
                        .Options;
                    using var threadDb = new AppDbContext(options);
                    threadDb.BosImportSessions.Add(new BosImportSession
                    {
                        BrokerageHouseId = 1,
                        FileType         = "Clients-UBR",
                        XmlFileName      = $"Clients-UBR-{index}.xml",
                        CtrlFileName     = $"Clients-UBR-{index}-ctrl.xml",
                        ExpectedMd5      = "abc",
                        ActualMd5        = "abc",
                        ImportedByUserId = 1,
                        Status           = "Reconciled",
                        ImportedAt       = DateTime.UtcNow
                    });
                    await threadDb.SaveChangesAsync();
                    lock (lockObj) { successCount++; }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, successCount);
        }

        // ── Race Condition: Simultaneous Order + Audit ────────────────────

        [Fact]
        public async Task SimultaneousOrderAndAudit_NeitherBlocksOther()
        {
            var db = CreateDb();
            var dbName = _sharedDbName;
            await SeedBasicDataAsync(db);
            var stock = new Stock { Id = 1, TradingCode = "SQPH", CompanyName = "Square Pharma", LastTradePrice = 200m, IsActive = true };
            db.Stocks.Add(stock);
            await db.SaveChangesAsync();

            var orderTask = Task.Run(async () =>
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options;
                using var threadDb = new AppDbContext(options);
                for (int i = 0; i < 5; i++)
                {
                    threadDb.Orders.Add(new Order
                    {
                        InvestorId = 1, BrokerageHouseId = 1, StockId = 1,
                        OrderType = OrderType.Buy, Quantity = 10, PriceAtOrder = 200m,
                        Status = OrderStatus.Pending,
                        
                    });
                }
                await threadDb.SaveChangesAsync();
            });

            var auditTask = Task.Run(async () =>
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options;
                using var threadDb = new AppDbContext(options);
                for (int i = 0; i < 5; i++)
                {
                    threadDb.AuditLogs.Add(new AuditLog
                    {
                        UserId = 1, Action = $"PlaceOrder_{i}", EntityType = "Order",
                        EntityId = i, CreatedAt = DateTime.UtcNow, IpAddress = "127.0.0.1"
                    });
                }
                await threadDb.SaveChangesAsync();
            });

            await Task.WhenAll(orderTask, auditTask);

            using var verifyDb = OpenDb(dbName);
            var orderCount = await verifyDb.Orders.CountAsync();
            var auditCount = await verifyDb.AuditLogs.CountAsync();
            Assert.Equal(5, orderCount);
            Assert.Equal(5, auditCount);
        }

        // ── Thread Safety: Multiple Readers ──────────────────────────────

        [Fact]
        public async Task MultipleReaders_SameData_NoCrossContamination()
        {
            var db1 = CreateDb();
            var db2 = CreateDb();

            await SeedBasicDataAsync(db1);
            await SeedBasicDataAsync(db2);

            var readTask1 = Task.Run(async () =>
            {
                var users = await db1.Users.ToListAsync();
                return users.Count;
            });

            var readTask2 = Task.Run(async () =>
            {
                var users = await db2.Users.ToListAsync();
                return users.Count;
            });

            var results = await Task.WhenAll(readTask1, readTask2);
            Assert.Equal(1, results[0]);
            Assert.Equal(1, results[1]);
        }

        // ── Deadlock Detection: Mutual Dependency ────────────────────────

        [Fact]
        public async Task NoDeadlock_WhenTwoThreadsWriteDifferentEntities()
        {
            var db = CreateDb();
            var dbName = _sharedDbName;
            await SeedBasicDataAsync(db);
            var stock = new Stock { Id = 1, TradingCode = "MTB", CompanyName = "Mutual Trust Bank", LastTradePrice = 30m, IsActive = true };
            db.Stocks.Add(stock);
            await db.SaveChangesAsync();

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var task1 = Task.Run(async () =>
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options;
                using var threadDb = new AppDbContext(options);
                threadDb.Orders.Add(new Order
                {
                    InvestorId = 1, BrokerageHouseId = 1, StockId = 1,
                    OrderType = OrderType.Buy, Quantity = 10, PriceAtOrder = 30m,
                    Status = OrderStatus.Pending, 
                });
                await threadDb.SaveChangesAsync(cts.Token);
            }, cts.Token);

            var task2 = Task.Run(async () =>
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options;
                using var threadDb = new AppDbContext(options);
                threadDb.AuditLogs.Add(new AuditLog
                {
                    UserId = 1, Action = "ParallelWrite", EntityType = "Stock",
                    EntityId = 1, CreatedAt = DateTime.UtcNow, IpAddress = "10.0.0.1"
                });
                await threadDb.SaveChangesAsync(cts.Token);
            }, cts.Token);

            var completed = await Task.WhenAll(task1, task2)
                                      .ContinueWith(t => !t.IsFaulted && !t.IsCanceled);
            Assert.True(completed);
    }

        // ── Extra Concurrency Tests ───────────────────────────────────────

        [Fact]
        public async Task ConcurrentStockReads_ReturnConsistentData()
        {
            var dbName = Guid.NewGuid().ToString();
            var db = CreateDb(dbName);
            db.Stocks.Add(new Stock { Id = 1, TradingCode = "GP", CompanyName = "Grameenphone", Exchange = "DSE", LastTradePrice = 350m, IsActive = true });
            await db.SaveChangesAsync();

            const int threadCount = 15;
            var prices = new System.Collections.Concurrent.ConcurrentBag<decimal>();
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var threadDb = OpenDb(dbName);
                    var stock = await threadDb.Stocks.FirstOrDefaultAsync(s => s.TradingCode == "GP");
                    if (stock != null) prices.Add(stock.LastTradePrice);
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, prices.Count);
            Assert.All(prices, p => Assert.Equal(350m, p));
        }

        [Fact]
        public async Task ConcurrentUserReads_AllReturnSameUser()
        {
            var dbName = Guid.NewGuid().ToString();
            var db = CreateDb(dbName);
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            db.Users.Add(new User { Id = 1, BrokerageHouseId = 1, RoleId = 1, FullName = "Test User", Email = "t@t.com", PasswordHash = "h" });
            await db.SaveChangesAsync();

            const int threadCount = 10;
            var names = new System.Collections.Concurrent.ConcurrentBag<string>();
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var threadDb = OpenDb(dbName);
                    var user = await threadDb.Users.FirstOrDefaultAsync(u => u.Id == 1);
                    if (user != null) names.Add(user.FullName);
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, names.Count);
            Assert.All(names, n => Assert.Equal("Test User", n));
        }

        [Fact]
        public async Task ConcurrentNotificationWrites_AllSaved()
        {
            var dbName = Guid.NewGuid().ToString();
            var db = CreateDb(dbName);
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            db.Users.Add(new User { Id = 1, BrokerageHouseId = 1, RoleId = 1, FullName = "Test", Email = "t@t.com", PasswordHash = "h" });
            await db.SaveChangesAsync();

            const int threadCount = 10;
            var tasks = new List<Task>();
            var successCount = 0;
            var lockObj = new object();

            for (int i = 0; i < threadCount; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    using var threadDb = OpenDb(dbName);
                    threadDb.Notifications.Add(new Notification
                    {
                        UserId    = 1,
                        Title     = $"Notification {index}",
                        Message   = $"Message {index}",
                        IsRead    = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    await threadDb.SaveChangesAsync();
                    lock (lockObj) { successCount++; }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, successCount);
        }

        [Fact]
        public async Task ConcurrentWatchlistReads_DoNotThrow()
        {
            var dbName = Guid.NewGuid().ToString();
            var db = CreateDb(dbName);
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            db.Users.Add(new User { Id = 1, BrokerageHouseId = 1, RoleId = 1, FullName = "Test", Email = "t@t.com", PasswordHash = "h" });
            await db.SaveChangesAsync();

            const int threadCount = 10;
            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var threadDb = OpenDb(dbName);
                        var watchlists = await threadDb.Watchlists
                            .Where(w => w.UserId == 1)
                            .ToListAsync();
                    }
                    catch (Exception ex) { exceptions.Add(ex); }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Empty(exceptions);
        }

        [Fact]
        public async Task ConcurrentMarketDataReads_AllSucceed()
        {
            var dbName = Guid.NewGuid().ToString();
            var db = CreateDb(dbName);
            db.Stocks.Add(new Stock { Id = 1, TradingCode = "GP", CompanyName = "Grameenphone", Exchange = "DSE", LastTradePrice = 350m, IsActive = true });
            db.MarketData.Add(new MarketData
            {
                StockId  = 1,
                Exchange = "DSE",
                Volume   = 10000,
                Close    = 350m
            });
            await db.SaveChangesAsync();

            const int threadCount = 10;
            var results = new System.Collections.Concurrent.ConcurrentBag<bool>();
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var threadDb = OpenDb(dbName);
                    var data = await threadDb.MarketData.FirstOrDefaultAsync(m => m.StockId == 1);
                    results.Add(data != null);
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, results.Count);
            Assert.All(results, r => Assert.True(r));
        }

        [Fact]
        public async Task ConcurrentFundRequestWrites_AllSaved()
        {
            var dbName = Guid.NewGuid().ToString();
            var db = CreateDb(dbName);
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            db.Users.Add(new User { Id = 1, BrokerageHouseId = 1, RoleId = 1, FullName = "Test", Email = "t@t.com", PasswordHash = "h" });
            await db.SaveChangesAsync();

            const int threadCount = 8;
            var tasks = new List<Task>();
            var successCount = 0;
            var lockObj = new object();

            for (int i = 0; i < threadCount; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    using var threadDb = OpenDb(dbName);
                    threadDb.FundRequests.Add(new FundRequest
                    {
                        InvestorId       = 1,
                        BrokerageHouseId = 1,
                        Amount           = 10000m * (index + 1),
                        PaymentMethod    = PaymentMethod.Cash,
                        Status           = FundRequestStatus.Pending
                    });
                    await threadDb.SaveChangesAsync();
                    lock (lockObj) { successCount++; }
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, successCount);
        }

        [Fact]
        public async Task IsolatedDatabases_DoNotShareData()
        {
            var dbName1 = Guid.NewGuid().ToString();
            var dbName2 = Guid.NewGuid().ToString();

            var db1 = CreateDb(dbName1);
            var db2 = CreateDb(dbName2);

            db1.Stocks.Add(new Stock { Id = 1, TradingCode = "GP", CompanyName = "Grameenphone", Exchange = "DSE", LastTradePrice = 350m, IsActive = true });
            await db1.SaveChangesAsync();

            var countInDb2 = await db2.Stocks.CountAsync();
            Assert.Equal(0, countInDb2);
        }

        [Fact]
        public async Task ConcurrentBrokerageHouseReads_AllReturnSameCount()
        {
            var dbName = Guid.NewGuid().ToString();
            var db = CreateDb(dbName);
            db.BrokerageHouses.AddRange(
                new BrokerageHouse { Id = 1, Name = "Alpha", LicenseNumber = "L1" },
                new BrokerageHouse { Id = 2, Name = "Beta",  LicenseNumber = "L2" },
                new BrokerageHouse { Id = 3, Name = "Gamma", LicenseNumber = "L3" }
            );
            await db.SaveChangesAsync();

            const int threadCount = 12;
            var counts = new System.Collections.Concurrent.ConcurrentBag<int>();
            var tasks = new List<Task>();

            for (int i = 0; i < threadCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var threadDb = OpenDb(dbName);
                    var count = await threadDb.BrokerageHouses.CountAsync();
                    counts.Add(count);
                }));
            }

            await Task.WhenAll(tasks);
            Assert.Equal(threadCount, counts.Count);
            Assert.All(counts, c => Assert.Equal(3, c));
        }

    }
}