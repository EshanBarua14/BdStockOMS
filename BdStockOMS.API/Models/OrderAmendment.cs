namespace BdStockOMS.API.Models;

public class OrderAmendment
{
    public int Id              { get; set; }
    public int OrderId         { get; set; }
    public int AmendedByUserId { get; set; }
    public int? OldQuantity    { get; set; }
    public int? NewQuantity    { get; set; }
    public decimal? OldPrice   { get; set; }
    public decimal? NewPrice   { get; set; }
    public string? Reason      { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public Order Order         { get; set; } = null!;
    public User AmendedBy      { get; set; } = null!;
}
