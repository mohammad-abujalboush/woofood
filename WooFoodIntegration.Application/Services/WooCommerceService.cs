using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using System.Net.Http;

namespace WooFoodIntegration.Application.Services
{
    public class WooCommerceService : IWooCommerceService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantService _tenantService;
        private readonly IEncryptionService _encryptionService;

        public WooCommerceService(IHttpClientFactory httpClientFactory, ITenantService tenantService, IEncryptionService encryptionService)
        {
            _httpClientFactory = httpClientFactory;
            _tenantService = tenantService;
            _encryptionService = encryptionService;
        }

        public async Task<bool> UpdateProductStockAsync(Guid tenantId, WooCommerceStockUpdateDto stockUpdateDto, CancellationToken cancellationToken)
        {
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId, cancellationToken);
            if (tenant == null) throw new KeyNotFoundException($"Tenant {tenantId} not found.");

            var credential = await _tenantService.GetTenantCredentialAsync(tenantId, "WooCommerce", cancellationToken);
            var decryptedApiKey = _encryptionService.Decrypt(credential.EncryptedApiKey, credential.Iv, credential.Salt);

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(tenant.WooCommerceBaseUrl);

            var byteArray = Encoding.ASCII.GetBytes(decryptedApiKey);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            var productUpdate = new
            {
                stock_quantity = stockUpdateDto.StockQuantity,
                manage_stock = stockUpdateDto.ManageStock
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(productUpdate), Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"wp-json/wc/v3/products/{stockUpdateDto.ProductId}", jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"WooCommerce API Error for Product {stockUpdateDto.ProductId}: {errorBody}");
                return false; 
            }

            return true;
        }
    }
}