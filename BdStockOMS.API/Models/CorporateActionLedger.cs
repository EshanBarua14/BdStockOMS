using System;

namespace BdStockOMS.API.Models
{
    public enum CorporateActionLedgerType
    {
        DividendCash,
        BonusShareCredit,
        RightsEntitlement
    }

    public class CorporateActionLedger
    {
        public int Id                        { get; set; }
        public int CorporateActionId         { get; set; }
        public int InvestorId                { get; set; }
        public int StockId                   { get; set; }
        public int BrokerageHouseId          { get; set; }
        public CorporateActionLedgerType EntryType { get; set; }
        public int    HoldingQtyAtRecord     { get; set; }
        public decimal ActionValue           { get; set; }
        public decimal CashAmount            { get; set; }
        public int    SharesAwarded          { get; set; }
        public string Notes                  { get; set; } = string.Empty;
        public DateTime ProcessedAt          { get; set; } = DateTime.UtcNow;

        public virtual CorporateAction CorporateAction { get; set; } = null!;
    }
}
