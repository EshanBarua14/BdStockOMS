using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models
{
    public class StockAnalytics
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StockId { get; set; }

        [Required]
        [MaxLength(10)]
        public string Exchange { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal Vwap { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal High52W { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Low52W { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Beta { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal AvgVolume30D { get; set; }

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(StockId))]
        public Stock Stock { get; set; } = null!;
    }
}
