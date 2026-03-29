using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.Models;

public class FIXMessageLog
{
    public int Id                  { get; set; }
    public int BrokerageHouseId    { get; set; }
    [MaxLength(10)]  public string MsgType    { get; set; } = string.Empty;
    [MaxLength(10)]  public string Direction  { get; set; } = string.Empty;
    [MaxLength(50)]  public string? ClOrdID   { get; set; }
    [MaxLength(50)]  public string? Symbol    { get; set; }
    [MaxLength(10)]  public string? OrdStatus { get; set; }
    public string RawMessage       { get; set; } = string.Empty;
    public int MsgSeqNum           { get; set; }
    public bool IsProcessed        { get; set; } = false;
    public string? ErrorMessage    { get; set; }
    public DateTime SentAt         { get; set; } = DateTime.UtcNow;
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
}
