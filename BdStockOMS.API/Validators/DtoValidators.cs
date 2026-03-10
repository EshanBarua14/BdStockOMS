using BdStockOMS.API.DTOs.Auth;
using BdStockOMS.API.DTOs.Order;
using BdStockOMS.API.Models;
using FluentValidation;

namespace BdStockOMS.API.Validators;

// ── Login ────────────────────────────────────────────────────────
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters.");
    }
}

// ── Register Brokerage ───────────────────────────────────────────
public class RegisterBrokerageDtoValidator : AbstractValidator<RegisterBrokerageDto>
{
    public RegisterBrokerageDtoValidator()
    {
        RuleFor(x => x.FirmName)
            .NotEmpty().WithMessage("Firm name is required.")
            .MaximumLength(100).WithMessage("Firm name must not exceed 100 characters.");

        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("License number is required.")
            .MaximumLength(50).WithMessage("License number must not exceed 50 characters.")
            .Matches(@"^[A-Za-z0-9\-]+$").WithMessage("License number contains invalid characters.");

        RuleFor(x => x.FirmEmail)
            .NotEmpty().WithMessage("Firm email is required.")
            .EmailAddress().WithMessage("Invalid firm email format.")
            .MaximumLength(100).WithMessage("Firm email must not exceed 100 characters.");

        RuleFor(x => x.FirmPhone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters.")
            .Matches(@"^[0-9+\-\s()]*$").WithMessage("Phone contains invalid characters.")
            .When(x => !string.IsNullOrEmpty(x.FirmPhone));

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Full name must contain only letters and spaces.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Admin email is required.")
            .EmailAddress().WithMessage("Invalid admin email format.")
            .MaximumLength(100).WithMessage("Admin email must not exceed 100 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");
    }
}

// ── Place Order ──────────────────────────────────────────────────
public class PlaceOrderDtoValidator : AbstractValidator<PlaceOrderDto>
{
    public PlaceOrderDtoValidator()
    {
        RuleFor(x => x.StockId)
            .GreaterThan(0).WithMessage("Stock ID must be greater than 0.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Quantity cannot exceed 1,000,000.");

        RuleFor(x => x.LimitPrice)
            .NotNull().WithMessage("Limit price is required for Limit orders.")
            .GreaterThan(0).WithMessage("Limit price must be greater than 0.")
            .When(x => x.OrderCategory == OrderCategory.Limit);

        RuleFor(x => x.LimitPrice)
            .Null().WithMessage("Limit price must not be set for Market orders.")
            .When(x => x.OrderCategory == OrderCategory.Market);

        RuleFor(x => x.InvestorId)
            .GreaterThan(0).WithMessage("Investor ID must be greater than 0.")
            .When(x => x.InvestorId.HasValue);
    }
}

// ── Cancel Order ─────────────────────────────────────────────────
public class CancelOrderDtoValidator : AbstractValidator<CancelOrderDto>
{
    public CancelOrderDtoValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Cancellation reason is required.")
            .MinimumLength(5).WithMessage("Reason must be at least 5 characters.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}
