using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class BosXmlService : IBosXmlService
    {
        private readonly AppDbContext _db;

        public BosXmlService(AppDbContext db)
        {
            _db = db;
        }

        public string ComputeMd5(string fileContent)
        {
            var bytes = Encoding.UTF8.GetBytes(fileContent);
            var hash = MD5.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public bool VerifyMd5(string fileContent, string expectedMd5)
        {
            var actual = ComputeMd5(fileContent);
            return string.Equals(actual, expectedMd5.Trim().ToLowerInvariant(),
                StringComparison.OrdinalIgnoreCase);
        }

        public string ExtractMd5FromCtrl(string ctrlContent)
        {
            try
            {
                var doc = XDocument.Parse(ctrlContent);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
                var md5 = doc.Descendants(ns + "MD5").FirstOrDefault()?.Value
                       ?? doc.Descendants(ns + "Checksum").FirstOrDefault()?.Value
                       ?? doc.Descendants(ns + "Hash").FirstOrDefault()?.Value
                       ?? doc.Descendants("MD5").FirstOrDefault()?.Value
                       ?? doc.Descendants("Checksum").FirstOrDefault()?.Value
                       ?? string.Empty;
                return md5.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        public List<BosClientRecord> ParseClientsXml(string xmlContent)
        {
            var records = new List<BosClientRecord>();
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
                var clients = ns == XNamespace.None ? doc.Descendants("Client") : doc.Descendants(ns + "Client");
                foreach (var c in clients)
                {
                    records.Add(new BosClientRecord
                    {
                        BoAccountNumber = c.Element(ns + "BOAccountNumber")?.Value
                                       ?? c.Element("BOAccountNumber")?.Value
                                       ?? c.Attribute("boAccountNo")?.Value
                                       ?? string.Empty,
                        ClientName      = c.Element(ns + "ClientName")?.Value
                                       ?? c.Element("ClientName")?.Value
                                       ?? string.Empty,
                        NidOrPassport   = c.Element(ns + "NID")?.Value
                                       ?? c.Element("NID")?.Value
                                       ?? c.Element("Passport")?.Value
                                       ?? string.Empty,
                        Email           = c.Element(ns + "Email")?.Value
                                       ?? c.Element("Email")?.Value
                                       ?? string.Empty,
                        Mobile          = c.Element(ns + "Mobile")?.Value
                                       ?? c.Element("Mobile")?.Value
                                       ?? string.Empty,
                    });
                }
            }
            catch { }
            return records;
        }

        public List<BosPositionRecord> ParsePositionsXml(string xmlContent)
        {
            var records = new List<BosPositionRecord>();
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
                var positions = ns == XNamespace.None ? doc.Descendants("Position") : doc.Descendants(ns + "Position");
                foreach (var p in positions)
                {
                    records.Add(new BosPositionRecord
                    {
                        BoAccountNumber = p.Element(ns + "BOAccountNumber")?.Value
                                       ?? p.Element("BOAccountNumber")?.Value
                                       ?? string.Empty,
                        StockCode       = p.Element(ns + "StockCode")?.Value
                                       ?? p.Element("StockCode")?.Value
                                       ?? p.Element("Symbol")?.Value
                                       ?? string.Empty,
                        Quantity        = decimal.TryParse(
                                            p.Element(ns + "Quantity")?.Value
                                         ?? p.Element("Quantity")?.Value, out var q) ? q : 0,
                        AveragePrice    = decimal.TryParse(
                                            p.Element(ns + "AveragePrice")?.Value
                                         ?? p.Element("AveragePrice")?.Value, out var ap) ? ap : 0,
                        MarketValue     = decimal.TryParse(
                                            p.Element(ns + "MarketValue")?.Value
                                         ?? p.Element("MarketValue")?.Value, out var mv) ? mv : 0,
                    });
                }
            }
            catch { }
            return records;
        }

        public async Task<BosReconciliationResult> ReconcileClientsAsync(BosUploadRequest request)
        {
            var session = new BosImportSession
            {
                BrokerageHouseId = request.BrokerageHouseId,
                FileType         = "Clients-UBR",
                XmlFileName      = request.XmlFileName,
                CtrlFileName     = request.CtrlFileName,
                ImportedByUserId = request.UploadedByUserId,
                ImportedAt       = DateTime.UtcNow,
                Status           = "Pending"
            };

            try
            {
                var expectedMd5 = ExtractMd5FromCtrl(request.CtrlContent);
                var actualMd5   = ComputeMd5(request.XmlContent);
                session.ExpectedMd5 = expectedMd5;
                session.ActualMd5   = actualMd5;
                session.Md5Verified = VerifyMd5(request.XmlContent, expectedMd5);

                if (!session.Md5Verified && !string.IsNullOrEmpty(expectedMd5))
                {
                    session.Status       = "Failed";
                    session.ErrorMessage = $"MD5 mismatch. Expected: {expectedMd5}, Actual: {actualMd5}";
                    _db.BosImportSessions.Add(session);
                    await _db.SaveChangesAsync();
                    return BuildResult(session, new List<string>());
                }

                var clients = ParseClientsXml(request.XmlContent);
                session.TotalRecords = clients.Count;

                var boNumbers = clients.Select(c => c.BoAccountNumber)
                                       .Where(b => !string.IsNullOrEmpty(b))
                                       .ToList();

                var matchedBo = await _db.Users
                    .Where(u => u.BrokerageHouseId == request.BrokerageHouseId
                             && boNumbers.Contains(u.BONumber))
                    .Select(u => u.BONumber)
                    .ToListAsync();

                var unmatched = boNumbers.Except(matchedBo).ToList();
                session.ReconciledRecords = matchedBo.Count;
                session.UnmatchedRecords  = unmatched.Count;
                session.Status            = "Reconciled";

                _db.BosImportSessions.Add(session);
                await _db.SaveChangesAsync();
                return BuildResult(session, unmatched);
            }
            catch (Exception ex)
            {
                session.Status       = "Failed";
                session.ErrorMessage = ex.Message;
                _db.BosImportSessions.Add(session);
                await _db.SaveChangesAsync();
                return BuildResult(session, new List<string>());
            }
        }

        public async Task<BosReconciliationResult> ReconcilePositionsAsync(BosUploadRequest request)
        {
            var session = new BosImportSession
            {
                BrokerageHouseId = request.BrokerageHouseId,
                FileType         = "Positions-UBR",
                XmlFileName      = request.XmlFileName,
                CtrlFileName     = request.CtrlFileName,
                ImportedByUserId = request.UploadedByUserId,
                ImportedAt       = DateTime.UtcNow,
                Status           = "Pending"
            };

            try
            {
                var expectedMd5 = ExtractMd5FromCtrl(request.CtrlContent);
                var actualMd5   = ComputeMd5(request.XmlContent);
                session.ExpectedMd5 = expectedMd5;
                session.ActualMd5   = actualMd5;
                session.Md5Verified = VerifyMd5(request.XmlContent, expectedMd5);

                if (!session.Md5Verified && !string.IsNullOrEmpty(expectedMd5))
                {
                    session.Status       = "Failed";
                    session.ErrorMessage = $"MD5 mismatch. Expected: {expectedMd5}, Actual: {actualMd5}";
                    _db.BosImportSessions.Add(session);
                    await _db.SaveChangesAsync();
                    return BuildResult(session, new List<string>());
                }

                var positions = ParsePositionsXml(request.XmlContent);
                session.TotalRecords = positions.Count;

                var boNumbers = positions.Select(p => p.BoAccountNumber)
                                         .Where(b => !string.IsNullOrEmpty(b))
                                         .Distinct()
                                         .ToList();

                var matchedBo = await _db.Users
                    .Where(u => u.BrokerageHouseId == request.BrokerageHouseId
                             && boNumbers.Contains(u.BONumber))
                    .Select(u => u.BONumber)
                    .ToListAsync();

                var unmatched = boNumbers.Except(matchedBo).ToList();
                session.ReconciledRecords = positions.Count(p => matchedBo.Contains(p.BoAccountNumber));
                session.UnmatchedRecords  = unmatched.Count;
                session.Status            = "Reconciled";

                _db.BosImportSessions.Add(session);
                await _db.SaveChangesAsync();
                return BuildResult(session, unmatched);
            }
            catch (Exception ex)
            {
                session.Status       = "Failed";
                session.ErrorMessage = ex.Message;
                _db.BosImportSessions.Add(session);
                await _db.SaveChangesAsync();
                return BuildResult(session, new List<string>());
            }
        }

        public async Task<BosExportResult> ExportPositionsToXmlAsync(int brokerageHouseId)
        {
            var portfolios = await _db.Portfolios
                .Include(p => p.Investor)
                .Include(p => p.Stock)
                .Where(p => p.Investor.BrokerageHouseId == brokerageHouseId && p.Quantity > 0)
                .ToListAsync();

            var xml = new XElement("Positions",
                new XAttribute("GeneratedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                new XAttribute("BrokerageHouseId", brokerageHouseId),
                portfolios.Select(p => new XElement("Position",
                    new XElement("BOAccountNumber", p.Investor.BONumber ?? string.Empty),
                    new XElement("StockCode",       p.Stock.TradingCode),
                    new XElement("Quantity",        p.Quantity),
                    new XElement("AveragePrice",    p.AverageBuyPrice),
                    new XElement("MarketValue",     p.Quantity * p.Stock.LastTradePrice)
                ))
            );

            var xmlContent = xml.ToString();
            var md5        = ComputeMd5(xmlContent);
            var fileName   = $"Positions-UBR-EOD-{DateTime.UtcNow:yyyyMMdd}.xml";

            return new BosExportResult
            {
                FileName   = fileName,
                XmlContent = xmlContent,
                Md5Hash    = md5
            };
        }

        public async Task<List<BosImportSession>> GetSessionsAsync(int brokerageHouseId)
        {
            return await _db.BosImportSessions
                .Where(s => s.BrokerageHouseId == brokerageHouseId)
                .OrderByDescending(s => s.ImportedAt)
                .Take(50)
                .ToListAsync();
        }

        private static BosReconciliationResult BuildResult(BosImportSession session, List<string> unmatched)
        {
            return new BosReconciliationResult
            {
                SessionId         = session.Id,
                Md5Verified       = session.Md5Verified,
                TotalRecords      = session.TotalRecords,
                ReconciledRecords = session.ReconciledRecords,
                UnmatchedRecords  = session.UnmatchedRecords,
                UnmatchedItems    = unmatched,
                Status            = session.Status,
                ErrorMessage      = session.ErrorMessage
            };
        }
    }
}
