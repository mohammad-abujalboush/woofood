using System.Security.Cryptography;
using System.Text;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;

namespace WooFoodIntegration.Application.Services
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ApiKeyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateApiKeyAsync(CreateApiKeyDto createApiKeyDto, CancellationToken cancellationToken)
        {
            var newKey = Guid.NewGuid().ToString("N"); // Generate a raw API key
            var hashedKey = HashApiKey(newKey);

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                TenantId = createApiKeyDto.TenantId,
                Key = hashedKey,
                Label = createApiKeyDto.Label,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = createApiKeyDto.ExpiresAt
            };

            await _unitOfWork.ApiKeys.AddAsync(apiKey, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return newKey; // Return the raw key to the user once
        }

        public async Task<ApiKey> GetApiKeyAsync(string key, CancellationToken cancellationToken)
        {
            var hashedKey = HashApiKey(key);
            return await _unitOfWork.ApiKeys.GetByKeyHashAsync(hashedKey, cancellationToken);
        }

        public async Task<bool> RevokeApiKeyAsync(string key, CancellationToken cancellationToken)
        {
            var hashedKey = HashApiKey(key);
            var apiKey = await _unitOfWork.ApiKeys.GetByKeyHashAsync(hashedKey, cancellationToken);

            if (apiKey == null)
            {
                return false;
            }

            await _unitOfWork.ApiKeys.RemoveAsync(apiKey, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);
            return true;
        }

        private string HashApiKey(string key)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}