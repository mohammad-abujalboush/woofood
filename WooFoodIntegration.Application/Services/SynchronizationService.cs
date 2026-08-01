using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WooFoodIntegration.Application.Services
{
    public class SynchronizationService : ISynchronizationService
    {
        private readonly IWooCommerceOrderMappingService _wooCommerceOrderMappingService;
        private readonly IWooCommerceService _wooCommerceService;
        private readonly IFoodicsService _foodicsService;
        private readonly IUnitOfWork _unitOfWork;

        public SynchronizationService(
            IWooCommerceOrderMappingService wooCommerceOrderMappingService,
            IWooCommerceService wooCommerceService,
            IFoodicsService foodicsService,
            IUnitOfWork unitOfWork)
        {
            _wooCommerceOrderMappingService = wooCommerceOrderMappingService;
            _wooCommerceService = wooCommerceService;
            _foodicsService = foodicsService;
            _unitOfWork = unitOfWork;
        }

        public async Task<SynchronizationLogDto> ProcessWooCommerceOrderCreatedAsync(Guid tenantId, WooCommerceOrderWebhookDto orderWebhook, CancellationToken cancellationToken)
        {
            var log = new SynchronizationLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventType = "OrderCreated",
                SourceSystem = "WooCommerce",
                SourceEntityId = orderWebhook.Id.ToString(),
                TargetSystem = "Foodics",
                TargetEntityId = string.Empty, // Initialize required property
                Status = "Pending",
                Message = "WooCommerce order received, starting Foodics order creation.",
                Timestamp = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.SynchronizationLogs.AddAsync(log, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            try
            {
                var foodicsOrderDto = _wooCommerceOrderMappingService.MapToFoodicsOrderCreateDto(orderWebhook, cancellationToken);
                var success = await _foodicsService.CreateOrderAsync(tenantId, foodicsOrderDto, cancellationToken);

                if (success)
                {
                    log.Status = "Success";
                    log.Message = "Foodics order created successfully.";
                    log.TargetEntityId = foodicsOrderDto.Reference; // Assuming Foodics returns a reference or we use WooCommerce ID as reference
                }
                else
                {
                    log.Status = "Failed";
                    log.Message = "Failed to create Foodics order.";
                }
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.Message = $"Error processing WooCommerce order: {ex.Message}";
            }

            log.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SynchronizationLogs.UpdateAsync(log, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new SynchronizationLogDto
            {
                Id = log.Id,
                TenantId = log.TenantId,
                EventType = log.EventType,
                SourceSystem = log.SourceSystem,
                SourceEntityId = log.SourceEntityId,
                TargetSystem = log.TargetSystem,
                TargetEntityId = log.TargetEntityId,
                Status = log.Status,
                Message = log.Message,
                Timestamp = log.Timestamp
            };
        }

        public async Task<SynchronizationLogDto> ProcessWooCommerceOrderUpdatedAsync(Guid tenantId, WooCommerceOrderWebhookDto orderWebhook, CancellationToken cancellationToken)
        {
            var log = new SynchronizationLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventType = "OrderUpdated",
                SourceSystem = "WooCommerce",
                SourceEntityId = orderWebhook.Id.ToString(),
                TargetSystem = "Foodics",
                TargetEntityId = string.Empty, // Initialize required property
                Status = "Pending",
                Message = "WooCommerce order updated webhook received, processing returns/refunds.",
                Timestamp = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.SynchronizationLogs.AddAsync(log, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            try
            {
                // Logic for processing returns/refunds and updating Foodics
                // This will be more complex and depend on how WooCommerce reports refunds/returns.
                // For now, let\'s assume if the order status is \'refunded\' or similar, we trigger a stock update in Foodics.
                if (orderWebhook.Status.Equals("refunded", StringComparison.OrdinalIgnoreCase) || orderWebhook.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    // Assuming a simple refund means stock goes back.
                    foreach (var lineItem in orderWebhook.LineItems)
                    {
                        var foodicsStockUpdate = new FoodicsStockUpdateDto
                        {
                            ProductReference = lineItem.ProductId,
                            NewQuantity = lineItem.Quantity // This would typically be a delta, not absolute
                        };
                        // In a real scenario, we\'d need to get current stock and add/subtract.
                        // For now, simulating an increase based on returned quantity.
                        var stockUpdateSuccess = await _foodicsService.UpdateProductStockAsync(tenantId, foodicsStockUpdate, cancellationToken);
                        if (!stockUpdateSuccess)
                        {
                            throw new Exception($"Failed to update Foodics stock for product {lineItem.ProductId}.");
                        }
                    }
                    log.Status = "Success";
                    log.Message = "WooCommerce return processed, Foodics stock updated.";
                }
                else
                {
                    log.Status = "Skipped";
                    log.Message = "WooCommerce order update not a recognized return/refund event.";
                }
            }
            catch (Exception ex)
            {
                log.Status = "Failed";
                log.Message = $"Error processing WooCommerce order update: {ex.Message}";
            }

            log.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SynchronizationLogs.UpdateAsync(log, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new SynchronizationLogDto
            {
                Id = log.Id,
                TenantId = log.TenantId,
                EventType = log.EventType,
                SourceSystem = log.SourceSystem,
                SourceEntityId = log.SourceEntityId,
                TargetSystem = log.TargetSystem,
                TargetEntityId = log.TargetEntityId,
                Status = log.Status,
                Message = log.Message,
                Timestamp = log.Timestamp
            };
        }

        public async Task<List<SynchronizationLogDto>> SyncFoodicsStockToWooCommerceAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            var logs = new List<SynchronizationLogDto>();
            var initialLog = new SynchronizationLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventType = "StockSync",
                SourceSystem = "Foodics",
                SourceEntityId = string.Empty, // Initialize required property
                TargetSystem = "WooCommerce",
                TargetEntityId = string.Empty, // Initialize required property
                Status = "Pending",
                Message = "Initiating stock synchronization from Foodics to WooCommerce.",
                Timestamp = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.SynchronizationLogs.AddAsync(initialLog, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            try
            {
                var foodicsStocks = await _foodicsService.GetFoodicsStockAsync(tenantId, cancellationToken);
                bool allIndividualUpdatesSuccessful = true;
                foreach (var stockItem in foodicsStocks)
                {
                    var updateLog = new SynchronizationLog
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        EventType = "StockUpdate",
                        SourceSystem = "Foodics",
                        SourceEntityId = stockItem.ProductReference,
                        TargetSystem = "WooCommerce",
                        TargetEntityId = string.Empty, // Initialize required property
                        Status = "Pending",
                        Message = $"Updating WooCommerce stock for product {stockItem.ProductReference} from Foodics.",
                        Timestamp = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.SynchronizationLogs.AddAsync(updateLog, cancellationToken);
                    await _unitOfWork.CompleteAsync(cancellationToken);

                    var wooCommerceStockUpdate = new WooCommerceStockUpdateDto
                    {
                        ProductId = stockItem.ProductReference, // Assuming ProductReference maps to WooCommerce Product ID
                        StockQuantity = stockItem.NewQuantity
                    };
                    var success = await _wooCommerceService.UpdateProductStockAsync(tenantId, wooCommerceStockUpdate, cancellationToken);

                    if (success)
                    {
                        updateLog.Status = "Success";
                        updateLog.Message = $"WooCommerce stock updated for product {stockItem.ProductReference} to {stockItem.NewQuantity}.";
                    }
                    else
                    {
                        updateLog.Status = "Failed";
                        updateLog.Message = $"Failed to update WooCommerce stock for product {stockItem.ProductReference}.";
                        allIndividualUpdatesSuccessful = false;
                    }

                    updateLog.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.SynchronizationLogs.UpdateAsync(updateLog, cancellationToken);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                    logs.Add(new SynchronizationLogDto
                    {
                        Id = updateLog.Id,
                        TenantId = updateLog.TenantId,
                        EventType = updateLog.EventType,
                        SourceSystem = updateLog.SourceSystem,
                        SourceEntityId = updateLog.SourceEntityId,
                        TargetSystem = updateLog.TargetSystem,
                        TargetEntityId = updateLog.TargetEntityId,
                        Status = updateLog.Status,
                        Message = updateLog.Message,
                        Timestamp = updateLog.Timestamp
                    });
                }

                if (allIndividualUpdatesSuccessful)
                {
                    initialLog.Status = "Success";
                    initialLog.Message = "Foodics stock synchronization to WooCommerce completed.";
                }
                else
                {
                    initialLog.Status = "Failed";
                    initialLog.Message = "Foodics stock synchronization to WooCommerce partially failed. See individual logs for details.";
                }
            }
            catch (Exception ex)
            {
                initialLog.Status = "Failed";
                initialLog.Message = $"Error during Foodics stock synchronization to WooCommerce: {ex.Message}";
            }
            initialLog.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.SynchronizationLogs.UpdateAsync(initialLog, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            logs.Insert(0, new SynchronizationLogDto // Add overall log at the beginning
            {
                Id = initialLog.Id,
                TenantId = initialLog.TenantId,
                EventType = initialLog.EventType,
                SourceSystem = initialLog.SourceSystem,
                SourceEntityId = initialLog.SourceEntityId,
                TargetSystem = initialLog.TargetSystem,
                TargetEntityId = initialLog.TargetEntityId,
                Status = initialLog.Status,
                Message = initialLog.Message,
                Timestamp = initialLog.Timestamp
            });

            return logs;
        }

        public async Task<SynchronizationLogDto> GetSynchronizationStatusAsync(Guid tenantId, Guid syncLogId, CancellationToken cancellationToken)
        {
            var log = await _unitOfWork.SynchronizationLogs.GetByIdAsync(syncLogId, cancellationToken);
            if (log == null || log.TenantId != tenantId) return null;

            return new SynchronizationLogDto
            {
                Id = log.Id,
                TenantId = log.TenantId,
                EventType = log.EventType,
                SourceSystem = log.SourceSystem,
                SourceEntityId = log.SourceEntityId,
                TargetSystem = log.TargetSystem,
                TargetEntityId = log.TargetEntityId,
                Status = log.Status,
                Message = log.Message,
                Timestamp = log.Timestamp
            };
        }
    }
}