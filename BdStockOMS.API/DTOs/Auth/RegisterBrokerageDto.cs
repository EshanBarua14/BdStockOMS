using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.Auth;

// DTO = Data Transfer Object
// This is what the CLIENT sends when
// registering a new brokerage house
// Only contains what we NEED — nothing extra
public class RegisterBrokerageDto
{
    // Brokerage firm details
    [Required(ErrorMessage = "Firm name is required")]
    [MaxLength(100)]
    public string FirmName { get; set; } = string.Empty;

    [Required(ErrorMessage = "License number is required")]
    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Firm email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string FirmEmail { get; set; } = string.Empty;

    [MaxLength(20)]
    public string FirmPhone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string FirmAddress { get; set; } = string.Empty;

    // Admin user details for the brokerage owner
    [Required(ErrorMessage = "Full name is required")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
}