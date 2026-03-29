using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models;

public class TenantFeatureFlag
{
    [Key]
    public int Id { get; set; }

    public int BrokerageHouseId { get; set; }

    [ForeignKey(nameof(BrokerageHouseId))]
    public virtual BrokerageHouse? BrokerageHouse { get; set; }

    [Required, MaxLength(100)]
    public string FeatureKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = false;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Value { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? SetByUserId { get; set; }
}
