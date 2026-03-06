using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.Auth;

// What client sends when logging in
// Simple — just email and password
public class LoginDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}