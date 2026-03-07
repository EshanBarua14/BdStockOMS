// DTOs/User/CreateUserDto.cs
using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.User;

public class CreateUserDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    // Allowed: Admin, Trader, Investor (BrokerageHouse creates these 3)
    public string Role { get; set; } = string.Empty;
}