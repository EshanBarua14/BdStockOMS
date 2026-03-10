using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public class BrokerageConnection
    {
        [Key]
        public int Id { get; set; }

        public int BrokerageHouseId { get; set; }

        [ForeignKey(nameof(BrokerageHouseId))]
        public virtual BrokerageHouse? BrokerageHouse { get; set; }

        [Required, MaxLength(500)]
        public string ConnectionString { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string DatabaseName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
