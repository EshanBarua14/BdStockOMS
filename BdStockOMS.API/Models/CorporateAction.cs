namespace BdStockOMS.API.Models;

public enum CorporateActionType
{
    Dividend,
    BonusShare,
    RightShare,
    StockSplit,
    Merger
}

public class CorporateAction
{
    public int Id                      { get; set; }
    public int StockId                 { get; set; }
    public CorporateActionType Type    { get; set; }
    public decimal Value               { get; set; } // dividend amount or ratio
    public DateTime RecordDate         { get; set; }
    public DateTime? PaymentDate       { get; set; }
    public string? Description         { get; set; }
    public bool IsProcessed            { get; set; } = false;
    public DateTime CreatedAt          { get; set; } = DateTime.UtcNow;

    // Navigation
    public Stock Stock { get; set; } = null!;
}
