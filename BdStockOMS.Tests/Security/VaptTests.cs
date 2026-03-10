using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BdStockOMS.Tests.Security
{
    // ============================================================
    //  IDOR Tests
    // ============================================================
    public class IdorTests : IClassFixture<BdStockOMS.Tests.Integration.BdStockOmsFactory>
    {
        private readonly HttpClient _client;

        public IdorTests(BdStockOMS.Tests.Integration.BdStockOmsFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task IDOR_Portfolio_OtherUser_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/Portfolio/9999/summary");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_Portfolio_NegativeId_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/Portfolio/-1/summary");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_KycEndpoint_OtherUser_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/Kyc/pending/9999");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_BosExport_OtherBrokerage_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/bos/export/positions/9999");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_Order_OtherUser_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/orders/9999");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_FundRequest_OtherUser_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/fund-requests");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_Watchlist_OtherUser_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/watchlists");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_AdminDashboard_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/AdminDashboard");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_AuditLogs_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/AuditCompliance/logs");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_BrokerageReport_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/BrokerageReport/1/orders");
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected 401/403/404 but got {response.StatusCode}");
        }

        [Fact]
        public async Task IDOR_PortfolioSnapshot_OtherUser_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/PortfolioSnapshot/history/9999");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task IDOR_UserEndpoint_OtherUser_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/users/9999");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    // ============================================================
    //  JWT Tampering Tests
    // ============================================================
    public class JwtTamperingTests : IClassFixture<BdStockOMS.Tests.Integration.BdStockOmsFactory>
    {
        private readonly HttpClient _client;

        public JwtTamperingTests(BdStockOMS.Tests.Integration.BdStockOmsFactory factory)
        {
            _client = factory.CreateClient();
        }

        private HttpClient WithToken(string token)
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return _client;
        }

        private void ClearToken() =>
            _client.DefaultRequestHeaders.Authorization = null;

        [Fact]
        public async Task JWT_NoneAlgorithm_Returns401()
        {
            // alg:none attack - header.payload.emptysig
            var header  = Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
            var payload = Base64UrlEncode("{\"sub\":\"1\",\"role\":\"SuperAdmin\",\"exp\":9999999999}");
            var token   = $"{header}.{payload}.";

            WithToken(token);
            var response = await _client.GetAsync("/api/auth/me");
            ClearToken();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task JWT_ExpiredToken_Returns401()
        {
            // exp in the past
            var header  = Base64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
            var payload = Base64UrlEncode("{\"sub\":\"1\",\"role\":\"Admin\",\"exp\":1}");
            var token   = $"{header}.{payload}.invalidsignature";

            WithToken(token);
            var response = await _client.GetAsync("/api/auth/me");
            ClearToken();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task JWT_WrongSignature_Returns401()
        {
            var header  = Base64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
            var payload = Base64UrlEncode("{\"sub\":\"1\",\"role\":\"SuperAdmin\",\"exp\":9999999999}");
            var token   = $"{header}.{payload}.WRONGSIGNATUREHERE";

            WithToken(token);
            var response = await _client.GetAsync("/api/auth/me");
            ClearToken();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task JWT_EmptyToken_Returns401()
        {
            WithToken("");
            var response = await _client.GetAsync("/api/auth/me");
            ClearToken();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task JWT_MalformedToken_OnePart_Returns401()
        {
            WithToken("notavalidjwt");
            var response = await _client.GetAsync("/api/auth/me");
            ClearToken();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task JWT_MalformedToken_TwoParts_Returns401()
        {
            WithToken("header.payload");
            var response = await _client.GetAsync("/api/auth/me");
            ClearToken();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task JWT_NullBearerValue_Returns401()
        {
            _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");
            var response = await _client.GetAsync("/api/auth/me");
            _client.DefaultRequestHeaders.Remove("Authorization");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task JWT_SqlInjectionInToken_Returns401()
        {
            WithToken("' OR 1=1; --");
            var response = await _client.GetAsync("/api/auth/me");
            ClearToken();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task JWT_TamperedPayload_EscalateRole_Returns401()
        {
            // valid-looking structure but payload tampered to SuperAdmin
            var header  = Base64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
            var payload = Base64UrlEncode("{\"sub\":\"999\",\"role\":\"SuperAdmin\",\"BrokerageHouseId\":\"1\",\"exp\":9999999999}");
            var token   = $"{header}.{payload}.tampered_signature";

            WithToken(token);
            var response = await _client.GetAsync("/api/auth/me");
            ClearToken();
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static string Base64UrlEncode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd("="[0]);
        }
    }

    // ============================================================
    //  Auth Bypass Tests
    // ============================================================
    public class AuthBypassTests : IClassFixture<BdStockOMS.Tests.Integration.BdStockOmsFactory>
    {
        private readonly HttpClient _client;

        public AuthBypassTests(BdStockOMS.Tests.Integration.BdStockOmsFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AuthBypass_NoToken_AdminDashboard_Returns401()
        {
            var response = await _client.GetAsync("/api/AdminDashboard");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_UsersList_Returns401()
        {
            var response = await _client.GetAsync("/api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_SystemSettings_Returns401()
        {
            var response = await _client.GetAsync("/api/SystemSetting");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_Notifications_Returns401()
        {
            var response = await _client.GetAsync("/api/Notification/logs");
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Expected 401/403/404 but got {response.StatusCode}");
        }

        [Fact]
        public async Task AuthBypass_NoToken_Orders_Returns401()
        {
            var response = await _client.GetAsync("/api/orders");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_RMS_Returns401()
        {
            var response = await _client.GetAsync("/api/rms/my-limits");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_Commission_Returns401()
        {
            var response = await _client.GetAsync("/api/commission/rates");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_MarketData_Returns401()
        {
            var response = await _client.GetAsync("/api/MarketData");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_BrokerageSettings_Returns401()
        {
            var response = await _client.GetAsync("/api/BrokerageSettings/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_HttpVerbTampering_PostToGetEndpoint_Returns401or405()
        {
            var response = await _client.PostAsJsonAsync("/api/orders", new { });
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.MethodNotAllowed ||
                response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task AuthBypass_NoToken_Logout_Returns401()
        {
            var response = await _client.PostAsync("/api/auth/logout", null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_GetMe_Returns401()
        {
            var response = await _client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_AuditCompliance_Returns401()
        {
            var response = await _client.GetAsync("/api/AuditCompliance/logs");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_FileImport_Returns401()
        {
            var response = await _client.PostAsync("/api/FileImport/stage", null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthBypass_NoToken_TenantProvisioning_Returns401()
        {
            var response = await _client.PostAsync("/api/TenantProvisioning/provision", null);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
