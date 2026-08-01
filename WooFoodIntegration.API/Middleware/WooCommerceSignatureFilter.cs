using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WooFoodIntegration.Application.Interfaces;

namespace WooFoodIntegration.API.Middleware
{
    public class WooCommerceSignatureFilter : IAsyncActionFilter
    {
        private readonly ITenantService _tenantService;
        private readonly IEncryptionService _encryptionService;

        public WooCommerceSignatureFilter(ITenantService tenantService, IEncryptionService encryptionService)
        {
            _tenantService = tenantService;
            _encryptionService = encryptionService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var request = context.HttpContext.Request;

            // 1. Verify that the signature header is present
            if (!request.Headers.TryGetValue("X-WC-Webhook-Signature", out var receivedSignature))
            {
                context.Result = new UnauthorizedObjectResult("Missing WooCommerce signature header.");
                return;
            }

            // 2. Extract the TenantId from the route parameters
            if (!context.RouteData.Values.TryGetValue("tenantId", out var tenantIdObj) || 
                !Guid.TryParse(tenantIdObj?.ToString(), out Guid tenantId))
            {
                context.Result = new BadRequestObjectResult("Invalid or missing Tenant ID in route.");
                return;
            }

            try
            {
                // 3. Retrieve and decrypt the secret webhook key for this tenant
                // Ensure "WooCommerceWebhookSecret" is the exact systemType you save the webhook secret under in the DB.
                var credential = await _tenantService.GetTenantCredentialAsync(tenantId, "WooCommerceWebhookSecret", context.HttpContext.RequestAborted);
                if (credential == null)
                {
                    context.Result = new UnauthorizedObjectResult("Webhook secret not configured for this tenant.");
                    return;
                }

                var webhookSecret = _encryptionService.Decrypt(credential.EncryptedApiKey, credential.Iv, credential.Salt);

                // 4. Read the raw HTTP request body safely
                request.EnableBuffering();
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var rawBody = await reader.ReadToEndAsync();
                request.Body.Position = 0; // Reset stream so the Controller can still read the JSON

                // 5. Compute the HMAC-SHA256 hash
                var secretBytes = Encoding.UTF8.GetBytes(webhookSecret);
                var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

                using var hmac = new HMACSHA256(secretBytes);
                var computedHashBytes = hmac.ComputeHash(bodyBytes);
                var computedSignature = Convert.ToBase64String(computedHashBytes);

                // 6. Compare computed signature against incoming header
                if (!string.Equals(computedSignature, receivedSignature, StringComparison.Ordinal))
                {
                    context.Result = new UnauthorizedObjectResult("Invalid WooCommerce signature header.");
                    return;
                }
            }
            catch (Exception ex)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                Console.WriteLine($"Error validating webhook signature: {ex.Message}");
                return;
            }

            await next();
        }
    }
}