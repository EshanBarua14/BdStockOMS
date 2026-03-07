// DTOs/CCD/BOAccountDto.cs
using System.ComponentModel.DataAnnotations;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.DTOs.CCD;

public class OpenBOAccountDto
{
    [Required]
    public int UserId { get; set; }

    [Required, MaxLength(20)]
    public string BONumber { get; set; } = string.Empty;

    [Required]
    public AccountType AccountType { get; set; }

    public decimal InitialCashBalance { get; set; } = 0;
    public decimal MarginLimit { get; set; } = 0;
}

public class DepositCashDto
{
    [Required]
    public int UserId { get; set; }

    [Required, Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
}

public class SetMarginLimitDto
{
    [Required]
    public int UserId { get; set; }

    [Required, Range(0, double.MaxValue)]
    public decimal MarginLimit { get; set; }
}

public class BOAccountResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? BONumber { get; set; }
    public string? AccountType { get; set; }
    public decimal CashBalance { get; set; }
    public decimal MarginLimit { get; set; }
    public decimal MarginUsed { get; set; }
    public decimal AvailableMargin { get; set; }
    public bool IsBOAccountActive { get; set; }
}
