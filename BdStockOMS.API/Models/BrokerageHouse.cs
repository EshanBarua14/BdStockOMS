using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.Models;

// Represents a registered brokerage firm
// Example: Pioneer Securities Ltd, ABC Brokerage
public class BrokerageHouse
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // DSE/CSE license number — unique per firm
    [Required]
    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    // [EmailAddress] validates format is a@b.com
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;

    // Is this brokerage active or suspended?
    // bool in C# = BIT in SQL Server
    // default = true (active when registered)
    public bool IsActive { get; set; } = true;

    // DateTime = DATETIME2 in SQL Server
    // DateTime.UtcNow = current UTC time automatically set
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // One BrokerageHouse has many Users
    public virtual ICollection<User> Users { get; set; }
        = new List<User>();
}