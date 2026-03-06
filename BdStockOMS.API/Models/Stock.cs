using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.Models;

// Represents a listed company on DSE or CSE
// Data comes from our scraper (Day 9)
public class Stock
{
    public int Id { get; set; }

    // Trading code = short name used on exchange
    // Example: GP = Grameenphone, BRACBANK = BRAC Bank
    [Required]
    [MaxLength(20)]
    public string TradingCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    // Which exchange — "DSE" or "CSE"
    [Required]
    [MaxLength(10)]
    public string Exchange { get; set; } = string.Empty;

    // decimal = precise money values (no floating point errors)
    // decimal(18,2) = up to 18 digits, 2 decimal places
    // Example: 380.50 taka
    public decimal LastTradePrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }

    // Change from previous close
    // Can be negative (price dropped) so decimal, not uint
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }

    // How many shares traded today
    public long Volume { get; set; }
    // long = bigger than int — volume can be millions

    // Total trade value in millions of taka
    public decimal ValueInMillionTaka { get; set; }

    // When scraper last updated this stock's data
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // One Stock can have many Orders placed on it
    public virtual ICollection<Order> Orders { get; set; }
        = new List<Order>();
}