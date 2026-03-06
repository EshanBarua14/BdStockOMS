using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.Models;

// This class becomes the "Roles" table in SQL Server
// Each property = one column in the table
public class Role
{
    // Primary Key — EF Core sees "Id" and makes it PK automatically
    public int Id { get; set; }

    // [Required] = NOT NULL in SQL
    // [MaxLength] = VARCHAR(50) in SQL
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    // = string.Empty prevents null warning in C# 8+
    // It means "default value is empty string"

    // Navigation property — one Role has many Users
    // EF Core uses this to do JOINs automatically
    // "virtual" allows lazy loading (load only when needed)
    public virtual ICollection<User> Users { get; set; }
        = new List<User>();
}