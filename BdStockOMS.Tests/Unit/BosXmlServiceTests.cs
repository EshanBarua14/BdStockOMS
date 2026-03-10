using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Xunit;

namespace BdStockOMS.Tests.Unit
{
    public class BosXmlServiceTests
    {
        private AppDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private BosXmlService CreateService(AppDbContext db) => new BosXmlService(db);

        // ── MD5 Tests ─────────────────────────────────────────────────────

        [Fact]
        public void ComputeMd5_ReturnslowercaseHex()
        {
            var svc = CreateService(CreateDb());
            var hash = svc.ComputeMd5("hello world");
            Assert.Equal(32, hash.Length);
            Assert.Equal(hash, hash.ToLowerInvariant());
        }

        [Fact]
        public void ComputeMd5_SameInput_ReturnsSameHash()
        {
            var svc = CreateService(CreateDb());
            var h1 = svc.ComputeMd5("test content");
            var h2 = svc.ComputeMd5("test content");
            Assert.Equal(h1, h2);
        }

        [Fact]
        public void ComputeMd5_DifferentInput_ReturnsDifferentHash()
        {
            var svc = CreateService(CreateDb());
            var h1 = svc.ComputeMd5("content A");
            var h2 = svc.ComputeMd5("content B");
            Assert.NotEqual(h1, h2);
        }

        [Fact]
        public void VerifyMd5_CorrectHash_ReturnsTrue()
        {
            var svc = CreateService(CreateDb());
            var content = "DSE BOS file content";
            var hash = svc.ComputeMd5(content);
            Assert.True(svc.VerifyMd5(content, hash));
        }

        [Fact]
        public void VerifyMd5_WrongHash_ReturnsFalse()
        {
            var svc = CreateService(CreateDb());
            Assert.False(svc.VerifyMd5("content", "00000000000000000000000000000000"));
        }

        [Fact]
        public void VerifyMd5_CaseInsensitive_ReturnsTrue()
        {
            var svc = CreateService(CreateDb());
            var content = "test";
            var hash = svc.ComputeMd5(content).ToUpperInvariant();
            Assert.True(svc.VerifyMd5(content, hash));
        }

        // ── ExtractMd5FromCtrl Tests ──────────────────────────────────────

        [Fact]
        public void ExtractMd5FromCtrl_MD5Element_ReturnsHash()
        {
            var svc = CreateService(CreateDb());
            var ctrl = "<Control><MD5>abc123</MD5></Control>";
            Assert.Equal("abc123", svc.ExtractMd5FromCtrl(ctrl));
        }

        [Fact]
        public void ExtractMd5FromCtrl_ChecksumElement_ReturnsHash()
        {
            var svc = CreateService(CreateDb());
            var ctrl = "<Control><Checksum>def456</Checksum></Control>";
            Assert.Equal("def456", svc.ExtractMd5FromCtrl(ctrl));
        }

        [Fact]
        public void ExtractMd5FromCtrl_InvalidXml_ReturnsEmpty()
        {
            var svc = CreateService(CreateDb());
            Assert.Equal(string.Empty, svc.ExtractMd5FromCtrl("not xml at all"));
        }

        [Fact]
        public void ExtractMd5FromCtrl_NoHashElement_ReturnsEmpty()
        {
            var svc = CreateService(CreateDb());
            var ctrl = "<Control><Other>value</Other></Control>";
            Assert.Equal(string.Empty, svc.ExtractMd5FromCtrl(ctrl));
        }

        // ── ParseClientsXml Tests ─────────────────────────────────────────

        [Fact]
        public void ParseClientsXml_ValidXml_ReturnsClients()
        {
            var svc = CreateService(CreateDb());
            var xml = @"<Clients>
                <Client>
                    <BOAccountNumber>1201000000000001</BOAccountNumber>
                    <ClientName>Rahim</ClientName>
                    <NID>1234567890</NID>
                    <Email>rahim@test.com</Email>
                    <Mobile>01700000001</Mobile>
                </Client>
                <Client>
                    <BOAccountNumber>1201000000000002</BOAccountNumber>
                    <ClientName>Karim</ClientName>
                    <NID>0987654321</NID>
                    <Email>karim@test.com</Email>
                    <Mobile>01700000002</Mobile>
                </Client>
            </Clients>";

            var result = svc.ParseClientsXml(xml);
            Assert.Equal(2, result.Count);
            Assert.Equal("1201000000000001", result[0].BoAccountNumber);
            Assert.Equal("Rahim", result[0].ClientName);
            Assert.Equal("1234567890", result[0].NidOrPassport);
        }

        [Fact]
        public void ParseClientsXml_EmptyXml_ReturnsEmptyList()
        {
            var svc = CreateService(CreateDb());
            var result = svc.ParseClientsXml("<Clients></Clients>");
            Assert.Empty(result);
        }

        [Fact]
        public void ParseClientsXml_MalformedXml_ReturnsEmptyList()
        {
            var svc = CreateService(CreateDb());
            var result = svc.ParseClientsXml("this is not xml");
            Assert.Empty(result);
        }

        [Fact]
        public void ParseClientsXml_SingleClient_ReturnsOneRecord()
        {
            var svc = CreateService(CreateDb());
            var xml = @"<Clients>
                <Client>
                    <BOAccountNumber>1201000000000099</BOAccountNumber>
                    <ClientName>Test User</ClientName>
                </Client>
            </Clients>";
            var result = svc.ParseClientsXml(xml);
            Assert.Single(result);
            Assert.Equal("1201000000000099", result[0].BoAccountNumber);
        }

        // ── ParsePositionsXml Tests ───────────────────────────────────────

        [Fact]
        public void ParsePositionsXml_ValidXml_ReturnsPositions()
        {
            var svc = CreateService(CreateDb());
            var xml = @"<Positions>
                <Position>
                    <BOAccountNumber>1201000000000001</BOAccountNumber>
                    <StockCode>GP</StockCode>
                    <Quantity>100</Quantity>
                    <AveragePrice>350.50</AveragePrice>
                    <MarketValue>35050.00</MarketValue>
                </Position>
            </Positions>";

            var result = svc.ParsePositionsXml(xml);
            Assert.Single(result);
            Assert.Equal("1201000000000001", result[0].BoAccountNumber);
            Assert.Equal("GP", result[0].StockCode);
            Assert.Equal(100, result[0].Quantity);
            Assert.Equal(350.50m, result[0].AveragePrice);
        }

        [Fact]
        public void ParsePositionsXml_EmptyXml_ReturnsEmptyList()
        {
            var svc = CreateService(CreateDb());
            var result = svc.ParsePositionsXml("<Positions></Positions>");
            Assert.Empty(result);
        }

        [Fact]
        public void ParsePositionsXml_MalformedXml_ReturnsEmptyList()
        {
            var svc = CreateService(CreateDb());
            var result = svc.ParsePositionsXml("bad xml");
            Assert.Empty(result);
        }

        [Fact]
        public void ParsePositionsXml_MultiplePositions_ReturnsAll()
        {
            var svc = CreateService(CreateDb());
            var xml = @"<Positions>
                <Position><BOAccountNumber>BO1</BOAccountNumber><StockCode>GP</StockCode><Quantity>10</Quantity><AveragePrice>100</AveragePrice><MarketValue>1000</MarketValue></Position>
                <Position><BOAccountNumber>BO2</BOAccountNumber><StockCode>BRAC</StockCode><Quantity>20</Quantity><AveragePrice>200</AveragePrice><MarketValue>4000</MarketValue></Position>
                <Position><BOAccountNumber>BO3</BOAccountNumber><StockCode>SQPH</StockCode><Quantity>30</Quantity><AveragePrice>300</AveragePrice><MarketValue>9000</MarketValue></Position>
            </Positions>";
            var result = svc.ParsePositionsXml(xml);
            Assert.Equal(3, result.Count);
        }

        // ── ReconcileClientsAsync Tests ───────────────────────────────────

        [Fact]
        public async Task ReconcileClientsAsync_Md5Mismatch_ReturnsFailedStatus()
        {
            var db = CreateDb();
            var svc = CreateService(db);

            var xml = "<Clients><Client><BOAccountNumber>BO1</BOAccountNumber><ClientName>Test</ClientName></Client></Clients>";
            var ctrl = "<Control><MD5>wronghash</MD5></Control>";

            var request = new BosUploadRequest
            {
                BrokerageHouseId = 1,
                UploadedByUserId = 1,
                XmlFileName      = "Clients-UBR.xml",
                CtrlFileName     = "Clients-UBR-ctrl.xml",
                XmlContent       = xml,
                CtrlContent      = ctrl
            };

            var result = await svc.ReconcileClientsAsync(request);
            Assert.Equal("Failed", result.Status);
            Assert.False(result.Md5Verified);
        }

        [Fact]
        public async Task ReconcileClientsAsync_NoCtrlHash_SkipsMd5_Reconciles()
        {
            var db = CreateDb();
            var brokerage = new BrokerageHouse { Id = 1, Name = "Test Brokerage", LicenseNumber = "LIC001" };
            var role = new Role { Id = 1, Name = "Investor" };
            db.BrokerageHouses.Add(brokerage);
            db.Roles.Add(role);
            db.Users.Add(new User
            {
                Id = 1, BrokerageHouseId = 1, RoleId = 1,
                FullName = "Investor One", Email = "inv1@test.com",
                PasswordHash = "hash", BONumber = "1201000000000001",
            });
            await db.SaveChangesAsync();

            var svc = CreateService(db);
            var xml = @"<Clients>
                <Client><BOAccountNumber>1201000000000001</BOAccountNumber><ClientName>Test</ClientName></Client>
            </Clients>";

            var request = new BosUploadRequest
            {
                BrokerageHouseId = 1,
                UploadedByUserId = 1,
                XmlFileName      = "Clients-UBR.xml",
                CtrlFileName     = "Clients-UBR-ctrl.xml",
                XmlContent       = xml,
                CtrlContent      = "<Control></Control>"
            };

            var result = await svc.ReconcileClientsAsync(request);
            Assert.Equal("Reconciled", result.Status);
            Assert.Equal(1, result.TotalRecords);
            Assert.Equal(1, result.ReconciledRecords);
            Assert.Equal(0, result.UnmatchedRecords);
        }

        [Fact]
        public async Task ReconcileClientsAsync_UnmatchedBo_ReturnsUnmatchedList()
        {
            var db = CreateDb();
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            await db.SaveChangesAsync();

            var svc = CreateService(db);
            var xml = @"<Clients>
                <Client><BOAccountNumber>UNKNOWN_BO</BOAccountNumber><ClientName>Ghost</ClientName></Client>
            </Clients>";

            var request = new BosUploadRequest
            {
                BrokerageHouseId = 1,
                UploadedByUserId = 1,
                XmlFileName      = "Clients-UBR.xml",
                CtrlFileName     = "Clients-UBR-ctrl.xml",
                XmlContent       = xml,
                CtrlContent      = "<Control></Control>"
            };

            var result = await svc.ReconcileClientsAsync(request);
            Assert.Equal("Reconciled", result.Status);
            Assert.Equal(1, result.UnmatchedRecords);
            Assert.Contains("UNKNOWN_BO", result.UnmatchedItems);
        }

        [Fact]
        public async Task ReconcileClientsAsync_SessionSavedToDb()
        {
            var db = CreateDb();
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            await db.SaveChangesAsync();

            var svc = CreateService(db);
            var request = new BosUploadRequest
            {
                BrokerageHouseId = 1,
                UploadedByUserId = 1,
                XmlFileName      = "Clients-UBR.xml",
                CtrlFileName     = "Clients-UBR-ctrl.xml",
                XmlContent       = "<Clients></Clients>",
                CtrlContent      = "<Control></Control>"
            };

            await svc.ReconcileClientsAsync(request);
            var session = await db.BosImportSessions.FirstOrDefaultAsync();
            Assert.NotNull(session);
            Assert.Equal("Clients-UBR", session.FileType);
            Assert.Equal(1, session.BrokerageHouseId);
        }

        // ── ReconcilePositionsAsync Tests ─────────────────────────────────

        [Fact]
        public async Task ReconcilePositionsAsync_Md5Mismatch_ReturnsFailedStatus()
        {
            var db = CreateDb();
            var svc = CreateService(db);

            var xml = "<Positions><Position><BOAccountNumber>BO1</BOAccountNumber><StockCode>GP</StockCode><Quantity>10</Quantity><AveragePrice>100</AveragePrice><MarketValue>1000</MarketValue></Position></Positions>";
            var ctrl = "<Control><MD5>badhash</MD5></Control>";

            var request = new BosUploadRequest
            {
                BrokerageHouseId = 1,
                UploadedByUserId = 1,
                XmlFileName      = "Positions-UBR.xml",
                CtrlFileName     = "Positions-UBR-ctrl.xml",
                XmlContent       = xml,
                CtrlContent      = ctrl
            };

            var result = await svc.ReconcilePositionsAsync(request);
            Assert.Equal("Failed", result.Status);
            Assert.False(result.Md5Verified);
        }

        [Fact]
        public async Task ReconcilePositionsAsync_MatchedBo_ReturnsReconciledStatus()
        {
            var db = CreateDb();
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            db.Users.Add(new User
            {
                Id = 1, BrokerageHouseId = 1, RoleId = 1,
                FullName = "Investor One", Email = "inv1@test.com",
                PasswordHash = "hash", BONumber = "BO001",
            });
            await db.SaveChangesAsync();

            var svc = CreateService(db);
            var xml = "<Positions><Position><BOAccountNumber>BO001</BOAccountNumber><StockCode>GP</StockCode><Quantity>50</Quantity><AveragePrice>300</AveragePrice><MarketValue>15000</MarketValue></Position></Positions>";

            var request = new BosUploadRequest
            {
                BrokerageHouseId = 1,
                UploadedByUserId = 1,
                XmlFileName      = "Positions-UBR.xml",
                CtrlFileName     = "Positions-UBR-ctrl.xml",
                XmlContent       = xml,
                CtrlContent      = "<Control></Control>"
            };

            var result = await svc.ReconcilePositionsAsync(request);
            Assert.Equal("Reconciled", result.Status);
            Assert.Equal(1, result.ReconciledRecords);
            Assert.Equal(0, result.UnmatchedRecords);
        }

        [Fact]
        public async Task ReconcilePositionsAsync_SessionSavedWithCorrectFileType()
        {
            var db = CreateDb();
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            await db.SaveChangesAsync();

            var svc = CreateService(db);
            var request = new BosUploadRequest
            {
                BrokerageHouseId = 1,
                UploadedByUserId = 1,
                XmlFileName      = "Positions-UBR.xml",
                CtrlFileName     = "Positions-UBR-ctrl.xml",
                XmlContent       = "<Positions></Positions>",
                CtrlContent      = "<Control></Control>"
            };

            await svc.ReconcilePositionsAsync(request);
            var session = await db.BosImportSessions.FirstOrDefaultAsync();
            Assert.NotNull(session);
            Assert.Equal("Positions-UBR", session.FileType);
        }

        // ── GetSessionsAsync Tests ────────────────────────────────────────

        [Fact]
        public async Task GetSessionsAsync_ReturnsSessionsForBrokerage()
        {
            var db = CreateDb();
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Admin" });
            db.Users.Add(new User { Id = 1, BrokerageHouseId = 1, RoleId = 1, FullName = "Admin", Email = "a@test.com", PasswordHash = "h" });
            db.BosImportSessions.AddRange(
                new BosImportSession { BrokerageHouseId = 1, FileType = "Clients-UBR", XmlFileName = "f1.xml", CtrlFileName = "f1-ctrl.xml", ExpectedMd5 = "a", ActualMd5 = "a", ImportedByUserId = 1, Status = "Reconciled" },
                new BosImportSession { BrokerageHouseId = 1, FileType = "Positions-UBR", XmlFileName = "f2.xml", CtrlFileName = "f2-ctrl.xml", ExpectedMd5 = "b", ActualMd5 = "b", ImportedByUserId = 1, Status = "Reconciled" },
                new BosImportSession { BrokerageHouseId = 2, FileType = "Clients-UBR", XmlFileName = "f3.xml", CtrlFileName = "f3-ctrl.xml", ExpectedMd5 = "c", ActualMd5 = "c", ImportedByUserId = 1, Status = "Reconciled" }
            );
            await db.SaveChangesAsync();

            var svc = CreateService(db);
            var sessions = await svc.GetSessionsAsync(1);
            Assert.Equal(2, sessions.Count);
            Assert.All(sessions, s => Assert.Equal(1, s.BrokerageHouseId));
        }

        [Fact]
        public async Task GetSessionsAsync_NoSessions_ReturnsEmptyList()
        {
            var db = CreateDb();
            var svc = CreateService(db);
            var sessions = await svc.GetSessionsAsync(99);
            Assert.Empty(sessions);
        }

        [Fact]
        public async Task GetSessionsAsync_OrderedByImportedAtDescending()
        {
            var db = CreateDb();
            db.BrokerageHouses.Add(new BrokerageHouse { Id = 1, Name = "Test", LicenseNumber = "L1" });
            db.Roles.Add(new Role { Id = 1, Name = "Admin" });
            db.Users.Add(new User { Id = 1, BrokerageHouseId = 1, RoleId = 1, FullName = "Admin", Email = "a@test.com", PasswordHash = "h" });
            db.BosImportSessions.AddRange(
                new BosImportSession { BrokerageHouseId = 1, FileType = "Clients-UBR", XmlFileName = "old.xml", CtrlFileName = "old-ctrl.xml", ExpectedMd5 = "x", ActualMd5 = "x", ImportedByUserId = 1, Status = "Reconciled", ImportedAt = DateTime.UtcNow.AddDays(-2) },
                new BosImportSession { BrokerageHouseId = 1, FileType = "Clients-UBR", XmlFileName = "new.xml", CtrlFileName = "new-ctrl.xml", ExpectedMd5 = "y", ActualMd5 = "y", ImportedByUserId = 1, Status = "Reconciled", ImportedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var svc = CreateService(db);
            var sessions = await svc.GetSessionsAsync(1);
            Assert.Equal("new.xml", sessions[0].XmlFileName);
        }
    }
}
