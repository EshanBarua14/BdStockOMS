// DTOs/User/UserResponseDto.cs
namespace BdStockOMS.API.DTOs.User;

public class UserResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? BrokerageHouseName { get; set; }
    public int? BrokerageHouseId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}