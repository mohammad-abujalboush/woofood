using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using System.Net.Http;

namespace WooFoodIntegration.Application.Services
{
    public class FoodicsService : IFoodicsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantService _tenantService;
        private readonly IEncryptionService _encryptionService;
        private readonly IConfiguration _configuration;

        public FoodicsService(IHttpClientFactory httpClientFactory, ITenantService tenantService, IEncryptionService encryptionService, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _tenantService = tenantService;
            _encryptionService = encryptionService;
            _configuration = configuration;
        }

        public async Task<bool> CreateOrderAsync(Guid tenantId, FoodicsOrderCreateDto orderDto, CancellationToken cancellationToken)
        {
            var client = await SetupFoodicsClientAsync(tenantId, cancellationToken);
            var jsonContent = new StringContent(JsonSerializer.Serialize(orderDto), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("v5/orders", jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Foodics API Error ({(int)response.StatusCode}): {errorBody}");
            }

            return true;
        }

        public async Task<bool> UpdateProductStockAsync(Guid tenantId, FoodicsStockUpdateDto stockUpdateDto, CancellationToken cancellationToken)
        {
            var client = await SetupFoodicsClientAsync(tenantId, cancellationToken);
            var jsonContent = new StringContent(JsonSerializer.Serialize(stockUpdateDto), Encoding.UTF8, "application/json");

            var response = await client.PutAsync($"v5/products/{stockUpdateDto.ProductReference}/stock", jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Foodics Stock Sync Error ({(int)response.StatusCode}): {errorBody}");
            }

            return true;
        }

        public async Task<List<FoodicsStockUpdateDto>> GetFoodicsStockAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            var client = await SetupFoodicsClientAsync(tenantId, cancellationToken);
            var response = await client.GetAsync("v5/products?include=inventory", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"Foodics Fetch Stock Error ({(int)response.StatusCode}): {errorBody}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var liveStockData = JsonSerializer.Deserialize<List<FoodicsStockUpdateDto>>(
                jsonResponse, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return liveStockData ?? new List<FoodicsStockUpdateDto>();
        }

        // --- HELPER METHODS ---

        private async Task<HttpClient> SetupFoodicsClientAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId, cancellationToken);
            if (tenant == null) throw new KeyNotFoundException($"Tenant {tenantId} not found.");

            var credential = await _tenantService.GetTenantCredentialAsync(tenantId, "Foodics", cancellationToken);
            if (credential == null || string.IsNullOrEmpty(credential.EncryptedApiKey)) 
            {
                throw new Exception("Foodics credentials not found for this tenant.");
            }

            var decryptedApiKey = _encryptionService.Decrypt(credential.EncryptedApiKey, credential.Iv, credential.Salt);

            var client = _httpClientFactory.CreateClient();
            
            var baseUrl = tenant.FoodicsBaseUrl.EndsWith("/") ? tenant.FoodicsBaseUrl : $"{tenant.FoodicsBaseUrl}/";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", decryptedApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
    }
}