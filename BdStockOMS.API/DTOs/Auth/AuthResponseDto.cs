namespace BdStockOMS.API.DTOs.Auth;

// What server sends BACK after successful login
// Contains the JWT token + basic user info
// NEVER contains password hash
public class AuthResponseDto
{
    // The JWT token — client stores this
    // and sends it with every request
    public string Token { get; set; } = string.Empty;

    // When does this token expire?
    public DateTime ExpiresAt { get; set; }

    // Basic user info — so frontend knows
    // who is logged in without decoding token
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int BrokerageHouseId { get; set; }
    public string BrokerageHouseName { get; set; } = string.Empty;
}