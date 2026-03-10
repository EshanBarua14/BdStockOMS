using System;
using System.Threading;
using System.Threading.Tasks;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BdStockOMS.API.Services
{
    public class BosComplianceHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BosComplianceHostedService> _logger;

        public BosComplianceHostedService(
            IServiceProvider serviceProvider,
            ILogger<BosComplianceHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BosComplianceHostedService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var nextMidnight = now.Date.AddDays(1);
                var delay = nextMidnight - now;

                _logger.LogInformation(
                    "BOS compliance next run at {NextRun} UTC (in {Minutes:F0} minutes).",
                    nextMidnight, delay.TotalMinutes);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (stoppingToken.IsCancellationRequested) break;

                await RunComplianceRefreshAsync();
            }

            _logger.LogInformation("BosComplianceHostedService stopped.");
        }

        private async Task RunComplianceRefreshAsync()
        {
            try
            {
                _logger.LogInformation("BOS compliance midnight refresh starting...");
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<IFlextradeBosComplianceService>();
                await service.RefreshAllAsync();
                _logger.LogInformation("BOS compliance midnight refresh completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BOS compliance midnight refresh failed.");
            }
        }
    }
}
