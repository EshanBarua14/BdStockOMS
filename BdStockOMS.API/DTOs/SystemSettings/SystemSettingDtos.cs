using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.SystemSettings;

public class SystemSettingResponseDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedByName { get; set; }
}

public class CreateSystemSettingDto
{
    [Required(ErrorMessage = "Key is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Key must be between 1 and 100 characters.")]
    [RegularExpression(@"^[a-z0-9_]+$", ErrorMessage = "Key must be lowercase letters, numbers and underscores only.")]
    public string Key { get; set; } = string.Empty;

    [Required(ErrorMessage = "Value is required.")]
    [MaxLength(1000, ErrorMessage = "Value cannot exceed 1000 characters.")]
    public string Value { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Category must be between 1 and 50 characters.")]
    public string Category { get; set; } = string.Empty;

    public bool IsEncrypted { get; set; } = false;
}

public class UpdateSystemSettingDto
{
    [Required(ErrorMessage = "Value is required.")]
    [MaxLength(1000, ErrorMessage = "Value cannot exceed 1000 characters.")]
    public string Value { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}
