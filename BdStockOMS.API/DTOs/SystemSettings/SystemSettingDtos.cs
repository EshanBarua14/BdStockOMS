namespace BdStockOMS.API.DTOs.SystemSettings;

// What the API returns for a setting
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

// Create a new setting
public class CreateSystemSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; } = false;
}

// Update an existing setting's value
public class UpdateSystemSettingDto
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
