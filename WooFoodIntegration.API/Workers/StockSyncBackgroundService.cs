using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.Domain.Repositories;

namespace WooFoodIntegration.API.Workers
{
    public class StockSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<StockSyncBackgroundService> _logger;
        
        // Set the timer interval (e.g., 15 minutes)
        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(15);

        public StockSyncBackgroundService(IServiceProvider serviceProvider, ILogger<StockSyncBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Stock Sync Background Worker started.");

            // PeriodicTimer is the modern, safe way to handle loops in background services
            using var timer = new PeriodicTimer(_syncInterval);

            try
            {
                // Run an immediate sync when the application first boots up
                await DoWorkAsync(stoppingToken);

                // Wait for the next 15-minute tick, then loop forever until the server turns off
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await DoWorkAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Stock Sync Background Worker is stopping gracefully.");
            }
        }

        private async Task DoWorkAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Waking up to sync stock from Foodics to WooCommerce...");

            // Create a fresh DI Scope to safely access the scoped database services
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var syncService = scope.ServiceProvider.GetRequiredService<ISynchronizationService>();

            try
            {
                // Fetch every restaurant/tenant in your database
                var tenants = await unitOfWork.Tenants.GetAllAsync(cancellationToken);

                foreach (var tenant in tenants)
                {
                    _logger.LogInformation($"Syncing stock for Tenant: {tenant.Id}");

                    try
                    {
                        // Trigger the exact same service logic your Controller uses
                        await syncService.SyncFoodicsStockToWooCommerceAsync(tenant.Id, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        // If one tenant fails (e.g., bad API key), catch it so it doesn't break the loop for other tenants
                        _logger.LogError($"Failed to sync stock for Tenant {tenant.Id}. Error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Fatal error during the background sync cycle: {ex.Message}");
            }
        }
    }
}