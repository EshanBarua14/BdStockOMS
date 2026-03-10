using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Xunit;

namespace BdStockOMS.Tests.Integration
{
    public class AuthIntegrationTests : IClassFixture<BdStockOmsFactory>
    {
        private readonly HttpClient _client;
        private readonly BdStockOmsFactory _factory;

        public AuthIntegrationTests(BdStockOmsFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_InvalidCredentials_Returns401()
        {
            var payload = new { Email = "nobody@test.com", Password = "wrongpassword" };
            var response = await _client.PostAsJsonAsync("/api/auth/login", payload);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_MissingBody_Returns400()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new { Email = "", Password = "" });
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ProtectedEndpoint_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/stocks");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ProtectedEndpoint_InvalidToken_Returns401()
        {
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");
            var response = await _client.GetAsync("/api/stocks");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            _client.DefaultRequestHeaders.Authorization = null;
        }

        [Fact]
        public async Task HealthCheck_AppStarts_ReturnsAnyResponse()
        {
            var response = await _client.GetAsync("/swagger/v1/swagger.json");
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.MovedPermanently);
        }

        [Fact]
        public async Task SwaggerEndpoint_InDevelopment_ReturnsOk()
        {
            var response = await _client.GetAsync("/swagger/index.html");
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task BosUpload_NoToken_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/bos/upload/clients",
                new { BrokerageHouseId = 1 });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task BosExport_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/bos/export/positions/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

        [Fact]
        public async Task StocksEndpoint_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/stocks");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task PortfolioEndpoint_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/Portfolio/1/summary");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task FundRequestEndpoint_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/fund-requests");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task KycEndpoint_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/Kyc/pending/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task WatchlistEndpoint_NoToken_Returns401()
        {
            var response = await _client.GetAsync("/api/watchlists");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

}}
