using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.API.Middleware;

namespace WooFoodIntegration.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhooksController : ControllerBase
    {
        private readonly ISynchronizationService _synchronizationService;

        public WebhooksController(ISynchronizationService synchronizationService)
        {
            _synchronizationService = synchronizationService;
        }

        [HttpPost("woocommerce/order-created/{tenantId}")]
        // [ServiceFilter(typeof(WooCommerceSignatureFilter))] // Still commented out for the save test
        public async Task<IActionResult> WooCommerceOrderCreated(Guid tenantId, CancellationToken cancellationToken)
        {
            if (Request.Body.CanSeek) Request.Body.Position = 0;

            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken);

            // 1. Protection against completely blank payloads
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return Ok(new { message = "Empty payload received. Ping successful." });
            }

            // 2. Catch the dummy "Ping" test
            if (requestBody.Contains("webhook_id"))
            {
                return Ok(new { message = "Webhook ping successful" });
            }

            // 3. Safely try to read the JSON
            try
            {
                var orderWebhook = JsonSerializer.Deserialize<WooCommerceOrderWebhookDto>(
                    requestBody, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (orderWebhook == null) return BadRequest("Invalid order payload");

                var log = await _synchronizationService.ProcessWooCommerceOrderCreatedAsync(tenantId, orderWebhook, cancellationToken);
                return Ok(log);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse WooCommerce JSON: {ex.Message}");
                return BadRequest("Invalid JSON format.");
            }
        }

        [HttpPost("woocommerce/order-updated/{tenantId}")]
        // [ServiceFilter(typeof(WooCommerceSignatureFilter))]
        public async Task<IActionResult> WooCommerceOrderUpdated(Guid tenantId, CancellationToken cancellationToken)
        {
            if (Request.Body.CanSeek) Request.Body.Position = 0;

            using var reader = new StreamReader(Request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return Ok(new { message = "Empty payload received. Ping successful." });
            }

            if (requestBody.Contains("webhook_id"))
            {
                return Ok(new { message = "Webhook ping successful" });
            }

            try
            {
                var orderWebhook = JsonSerializer.Deserialize<WooCommerceOrderWebhookDto>(
                    requestBody, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (orderWebhook == null) return BadRequest("Invalid order payload");

                var log = await _synchronizationService.ProcessWooCommerceOrderUpdatedAsync(tenantId, orderWebhook, cancellationToken);
                return Ok(log);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse WooCommerce JSON: {ex.Message}");
                return BadRequest("Invalid JSON format.");
            }
        }
    }
}