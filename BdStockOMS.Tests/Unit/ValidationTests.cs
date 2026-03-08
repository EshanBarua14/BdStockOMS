using System.ComponentModel.DataAnnotations;
using BdStockOMS.API.DTOs.MarketData;
using BdStockOMS.API.DTOs.CorporateAction;
using BdStockOMS.API.DTOs.News;
using BdStockOMS.API.DTOs.OrderAmendment;
using BdStockOMS.API.DTOs.TraderReassignment;
using BdStockOMS.API.DTOs.SystemSettings;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class ValidationTests
{
    // Helper: validates a DTO and returns list of error messages
    private static List<string> Validate(object dto)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage ?? string.Empty).ToList();
    }

    // ── MARKET DATA VALIDATION ─────────────────────

    [Fact]
    public void CreateMarketDataDto_ValidInput_PassesValidation()
    {
        var dto = new CreateMarketDataDto
        {
            StockId = 1, Exchange = "DSE", Open = 375m, High = 385m,
            Low = 370m, Close = 380m, Volume = 100000,
            ValueInMillionTaka = 38m, Trades = 500, Date = DateTime.UtcNow.Date
        };

        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateMarketDataDto_EmptyExchange_FailsValidation()
    {
        var dto = new CreateMarketDataDto
        {
            StockId = 1, Exchange = "", Open = 375m, High = 385m,
            Low = 370m, Close = 380m, Date = DateTime.UtcNow.Date
        };

        var errors = Validate(dto);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Exchange"));
    }

    [Fact]
    public void CreateMarketDataDto_NegativeStockId_FailsValidation()
    {
        var dto = new CreateMarketDataDto
        {
            StockId = -1, Exchange = "DSE", Open = 375m,
            High = 385m, Low = 370m, Close = 380m, Date = DateTime.UtcNow.Date
        };

        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    // ── CORPORATE ACTION VALIDATION ────────────────

    [Fact]
    public void CreateCorporateActionDto_ValidInput_PassesValidation()
    {
        var dto = new CreateCorporateActionDto
        {
            StockId = 1, Type = "Dividend", Value = 5.0m,
            RecordDate = DateTime.UtcNow.AddDays(30)
        };

        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateCorporateActionDto_EmptyType_FailsValidation()
    {
        var dto = new CreateCorporateActionDto
        {
            StockId = 1, Type = "", Value = 5.0m,
            RecordDate = DateTime.UtcNow.AddDays(30)
        };

        var errors = Validate(dto);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Type"));
    }

    [Fact]
    public void CreateCorporateActionDto_ZeroValue_FailsValidation()
    {
        var dto = new CreateCorporateActionDto
        {
            StockId = 1, Type = "Dividend", Value = 0m,
            RecordDate = DateTime.UtcNow.AddDays(30)
        };

        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    // ── NEWS VALIDATION ────────────────────────────

    [Fact]
    public void CreateNewsDto_ValidInput_PassesValidation()
    {
        var dto = new CreateNewsDto
        {
            Title = "Market Update", Content = "The market rose today significantly.",
            Category = "MarketUpdate", IsPublished = true
        };

        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateNewsDto_EmptyTitle_FailsValidation()
    {
        var dto = new CreateNewsDto
        {
            Title = "", Content = "Some content here for the news item.",
            Category = "General"
        };

        var errors = Validate(dto);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Title"));
    }

    [Fact]
    public void CreateNewsDto_ShortContent_FailsValidation()
    {
        var dto = new CreateNewsDto
        {
            Title = "Valid Title", Content = "Short",
            Category = "General"
        };

        var errors = Validate(dto);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Content"));
    }

    // ── ORDER AMENDMENT VALIDATION ─────────────────

    [Fact]
    public void AmendOrderDto_ValidQuantity_PassesValidation()
    {
        var dto = new AmendOrderDto { NewQuantity = 100 };
        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void AmendOrderDto_ZeroQuantity_FailsValidation()
    {
        var dto = new AmendOrderDto { NewQuantity = 0 };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void AmendOrderDto_NegativePrice_FailsValidation()
    {
        var dto = new AmendOrderDto { NewPrice = -10m };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    // ── TRADER REASSIGNMENT VALIDATION ────────────

    [Fact]
    public void CreateTraderReassignmentDto_ValidInput_PassesValidation()
    {
        var dto = new CreateTraderReassignmentDto
        {
            InvestorId = 1, NewTraderId = 2, Reason = "Trader on leave"
        };

        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateTraderReassignmentDto_ZeroInvestorId_FailsValidation()
    {
        var dto = new CreateTraderReassignmentDto { InvestorId = 0, NewTraderId = 2 };
        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }

    // ── SYSTEM SETTING VALIDATION ──────────────────

    [Fact]
    public void CreateSystemSettingDto_ValidInput_PassesValidation()
    {
        var dto = new CreateSystemSettingDto
        {
            Key = "market_open_time", Value = "10:00",
            Category = "Trading", Description = "Market open time"
        };

        var errors = Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateSystemSettingDto_InvalidKeyFormat_FailsValidation()
    {
        var dto = new CreateSystemSettingDto
        {
            Key = "Market Open Time", Value = "10:00", Category = "Trading"
        };

        var errors = Validate(dto);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("lowercase"));
    }

    [Fact]
    public void CreateSystemSettingDto_EmptyKey_FailsValidation()
    {
        var dto = new CreateSystemSettingDto
        {
            Key = "", Value = "10:00", Category = "Trading"
        };

        var errors = Validate(dto);
        Assert.NotEmpty(errors);
    }
}
