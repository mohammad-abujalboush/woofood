using Microsoft.AspNetCore.Mvc;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.API.Middleware;

namespace WooFoodIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    public class SynchronizationController : ControllerBase
    {
        private readonly ISynchronizationService _synchronizationService;

        public SynchronizationController(ISynchronizationService synchronizationService)
        {
            _synchronizationService = synchronizationService;
        }

        /// <summary>
        /// Manually triggers synchronization of stock from Foodics to WooCommerce.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant for which to sync stock.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A list of synchronization log entries.</returns>
        [HttpPost("stock/foodics-to-woocommerce/{tenantId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SynchronizationLogDto>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SyncFoodicsStockToWooCommerce(Guid tenantId, CancellationToken cancellationToken)
        {
            // TenantId will typically be extracted from the authenticated API key or a specific request header.
            // For simplicity, it's passed in the route for now.
            try
            {
                var logs = await _synchronizationService.SyncFoodicsStockToWooCommerceAsync(tenantId, cancellationToken);
                return Ok(logs);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the status of a specific synchronization event.
        /// </summary>
        /// <param name="tenantId">The ID of the tenant.</param>
        /// <param name="syncLogId">The ID of the synchronization log entry.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The synchronization log details.</returns>
        [HttpGet("status/{tenantId}/{syncLogId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SynchronizationLogDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSynchronizationStatus(Guid tenantId, Guid syncLogId, CancellationToken cancellationToken)
        {
            var log = await _synchronizationService.GetSynchronizationStatusAsync(tenantId, syncLogId, cancellationToken);
            if (log == null)
            {
                return NotFound($"Synchronization log with ID {syncLogId} for tenant {tenantId} not found.");
            }
            return Ok(log);
        }
    }
}