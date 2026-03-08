namespace BdStockOMS.API.Models;

public class SectorConfig
{
    public int Id                  { get; set; }
    public string SectorName       { get; set; } = string.Empty; // Bank, Pharma etc
    public string SectorCode       { get; set; } = string.Empty;
    public decimal MaxConcentrationPct { get; set; } // BSEC 10% rule
    public bool IsActive           { get; set; } = true;
    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
}
