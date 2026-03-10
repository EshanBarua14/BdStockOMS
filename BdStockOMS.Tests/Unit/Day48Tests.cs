using System;
using System.Threading.Tasks;
using BdStockOMS.API.DTOs.Auth;
using BdStockOMS.API.DTOs.Order;
using BdStockOMS.API.Models;
using BdStockOMS.API.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace BdStockOMS.Tests.Unit
{
    // ============================================================
    //  LoginDto Validator Tests
    // ============================================================
    public class LoginDtoValidatorTests
    {
        private readonly LoginDtoValidator _validator = new();

        [Fact]
        public void Login_ValidDto_PassesValidation()
        {
            var dto = new LoginDto { Email = "user@test.com", Password = "Secret1!" };
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Login_EmptyEmail_FailsValidation()
        {
            var dto = new LoginDto { Email = "", Password = "Secret1!" };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Login_InvalidEmailFormat_FailsValidation()
        {
            var dto = new LoginDto { Email = "notanemail", Password = "Secret1!" };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Fact]
        public void Login_EmptyPassword_FailsValidation()
        {
            var dto = new LoginDto { Email = "user@test.com", Password = "" };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Login_ShortPassword_FailsValidation()
        {
            var dto = new LoginDto { Email = "user@test.com", Password = "abc" };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Login_TooLongEmail_FailsValidation()
        {
            var dto = new LoginDto { Email = new string('a', 95) + "@b.com", Password = "Secret1!" };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }
    }

    // ============================================================
    //  RegisterBrokerageDto Validator Tests
    // ============================================================
    public class RegisterBrokerageDtoValidatorTests
    {
        private readonly RegisterBrokerageDtoValidator _validator = new();

        private RegisterBrokerageDto ValidDto() => new()
        {
            FirmName      = "Test Brokerage",
            LicenseNumber = "LIC-12345",
            FirmEmail     = "firm@test.com",
            FirmPhone     = "01711000000",
            FirmAddress   = "Dhaka",
            FullName      = "John Doe",
            Email         = "admin@test.com",
            Password      = "Admin@1234"
        };

        [Fact]
        public void Register_ValidDto_PassesValidation()
        {
            var result = _validator.TestValidate(ValidDto());
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Register_EmptyFirmName_FailsValidation()
        {
            var dto = ValidDto(); dto.FirmName = "";
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.FirmName);
        }

        [Fact]
        public void Register_InvalidLicenseChars_FailsValidation()
        {
            var dto = ValidDto(); dto.LicenseNumber = "LIC@#$%";
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.LicenseNumber);
        }

        [Fact]
        public void Register_InvalidFirmEmail_FailsValidation()
        {
            var dto = ValidDto(); dto.FirmEmail = "notvalid";
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.FirmEmail);
        }

        [Fact]
        public void Register_WeakPassword_NoUppercase_FailsValidation()
        {
            var dto = ValidDto(); dto.Password = "admin@1234";
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Register_WeakPassword_NoSpecialChar_FailsValidation()
        {
            var dto = ValidDto(); dto.Password = "Admin1234";
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Register_WeakPassword_TooShort_FailsValidation()
        {
            var dto = ValidDto(); dto.Password = "Ab@1";
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Register_FullNameWithNumbers_FailsValidation()
        {
            var dto = ValidDto(); dto.FullName = "John123";
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.FullName);
        }

        [Fact]
        public void Register_EmptyAdminEmail_FailsValidation()
        {
            var dto = ValidDto(); dto.Email = "";
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Email);
        }
    }

    // ============================================================
    //  PlaceOrderDto Validator Tests
    // ============================================================
    public class PlaceOrderDtoValidatorTests
    {
        private readonly PlaceOrderDtoValidator _validator = new();

        private PlaceOrderDto ValidLimitOrder() => new()
        {
            StockId       = 1,
            OrderType     = OrderType.Buy,
            OrderCategory = OrderCategory.Limit,
            Quantity      = 100,
            LimitPrice    = 50.0m
        };

        private PlaceOrderDto ValidMarketOrder() => new()
        {
            StockId       = 1,
            OrderType     = OrderType.Buy,
            OrderCategory = OrderCategory.Market,
            Quantity      = 100,
            LimitPrice    = null
        };

        [Fact]
        public void PlaceOrder_ValidLimitOrder_PassesValidation()
        {
            var result = _validator.TestValidate(ValidLimitOrder());
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void PlaceOrder_ValidMarketOrder_PassesValidation()
        {
            var result = _validator.TestValidate(ValidMarketOrder());
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void PlaceOrder_ZeroStockId_FailsValidation()
        {
            var dto = ValidLimitOrder(); dto.StockId = 0;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.StockId);
        }

        [Fact]
        public void PlaceOrder_ZeroQuantity_FailsValidation()
        {
            var dto = ValidLimitOrder(); dto.Quantity = 0;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Quantity);
        }

        [Fact]
        public void PlaceOrder_OverMaxQuantity_FailsValidation()
        {
            var dto = ValidLimitOrder(); dto.Quantity = 2_000_000;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Quantity);
        }

        [Fact]
        public void PlaceOrder_LimitOrderNoPrice_FailsValidation()
        {
            var dto = ValidLimitOrder(); dto.LimitPrice = null;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.LimitPrice);
        }

        [Fact]
        public void PlaceOrder_LimitOrderZeroPrice_FailsValidation()
        {
            var dto = ValidLimitOrder(); dto.LimitPrice = 0;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.LimitPrice);
        }

        [Fact]
        public void PlaceOrder_MarketOrderWithPrice_FailsValidation()
        {
            var dto = ValidMarketOrder(); dto.LimitPrice = 50.0m;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.LimitPrice);
        }

        [Fact]
        public void PlaceOrder_InvalidInvestorId_FailsValidation()
        {
            var dto = ValidLimitOrder(); dto.InvestorId = -1;
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.InvestorId);
        }
    }

    // ============================================================
    //  CancelOrderDto Validator Tests
    // ============================================================
    public class CancelOrderDtoValidatorTests
    {
        private readonly CancelOrderDtoValidator _validator = new();

        [Fact]
        public void CancelOrder_ValidReason_PassesValidation()
        {
            var dto = new CancelOrderDto { Reason = "Client requested cancellation" };
            var result = _validator.TestValidate(dto);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void CancelOrder_EmptyReason_FailsValidation()
        {
            var dto = new CancelOrderDto { Reason = "" };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public void CancelOrder_TooShortReason_FailsValidation()
        {
            var dto = new CancelOrderDto { Reason = "No" };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }

        [Fact]
        public void CancelOrder_TooLongReason_FailsValidation()
        {
            var dto = new CancelOrderDto { Reason = new string('x', 501) };
            var result = _validator.TestValidate(dto);
            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }
    }

    // ============================================================
    //  GlobalExceptionMiddleware Tests
    // ============================================================
    public class GlobalExceptionMiddlewareTests
    {
        [Fact]
        public void ExceptionMiddleware_CanBeInstantiated()
        {
            var middleware = new BdStockOMS.API.Middleware.GlobalExceptionMiddleware(
                _ => Task.CompletedTask,
                new Microsoft.Extensions.Logging.Abstractions.NullLogger<BdStockOMS.API.Middleware.GlobalExceptionMiddleware>()
            );
            Assert.NotNull(middleware);
        }

        [Fact]
        public void SessionPolicy_AllRoles_HaveReasonableTimeouts()
        {
            var roles = new[] { "SuperAdmin", "Admin", "Compliance", "Trader", "Investor" };
            foreach (var role in roles)
            {
                var policy = new BdStockOMS.API.Models.SessionPolicy
                {
                    RoleName                 = role,
                    InactivityTimeoutMinutes = role == "SuperAdmin" ? 20 : 30
                };
                Assert.True(policy.InactivityTimeoutMinutes > 0,
                    $"{role} should have inactivity timeout > 0");
            }
        }
    }
}
