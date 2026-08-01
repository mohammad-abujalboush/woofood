using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;
using System.Security.Cryptography;

namespace WooFoodIntegration.Application.Services
{
    public class TenantService : ITenantService
    {
        private readonly IUnitOfWork _unitOfWork; 
        private readonly IEncryptionService _encryptionService;

        public TenantService(IUnitOfWork unitOfWork, IEncryptionService encryptionService)
        {
            _unitOfWork = unitOfWork;
            _encryptionService = encryptionService;
        }

        public async Task<TenantDto> CreateTenantAsync(CreateTenantDto createTenantDto, CancellationToken cancellationToken)
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = createTenantDto.Name,
                WooCommerceBaseUrl = createTenantDto.WooCommerceBaseUrl,
                FoodicsBaseUrl = createTenantDto.FoodicsBaseUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                WooCommerceBaseUrl = tenant.WooCommerceBaseUrl,
                FoodicsBaseUrl = tenant.FoodicsBaseUrl,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };
        }

        public async Task<TenantDto> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null) return null;

            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                WooCommerceBaseUrl = tenant.WooCommerceBaseUrl,
                FoodicsBaseUrl = tenant.FoodicsBaseUrl,
                CreatedAt = tenant.CreatedAt,
                UpdatedAt = tenant.UpdatedAt
            };
        }

        public async Task UpdateTenantCredentialsAsync(Guid tenantId, CreateTenantCredentialDto credentialsDto, CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                throw new KeyNotFoundException($"Tenant with ID {tenantId} not found.");
            }

            // Encrypt the API key (Generates IV and Salt)
            var (encryptedKey, iv, salt) = _encryptionService.Encrypt(credentialsDto.ApiKey);

            var existingCredential = await _unitOfWork.TenantCredentials.GetByTenantIdAndSystemTypeAsync(tenantId, credentialsDto.SystemType, cancellationToken);

            if (existingCredential != null)
            {
                existingCredential.EncryptedApiKey = encryptedKey;
                existingCredential.ApiSecret = credentialsDto.ApiSecret; // Add the secret plainly
                existingCredential.Iv = iv;
                existingCredential.Salt = salt;
                existingCredential.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.TenantCredentials.UpdateAsync(existingCredential, cancellationToken);
            }
            else
            {
                var newCredential = new TenantCredential
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SystemType = credentialsDto.SystemType,
                    EncryptedApiKey = encryptedKey,
                    ApiSecret = credentialsDto.ApiSecret, // Add the secret plainly
                    Iv = iv,
                    Salt = salt,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.TenantCredentials.AddAsync(newCredential, cancellationToken);
            }
            await _unitOfWork.CompleteAsync(cancellationToken);
        }

        public async Task<TenantCredential> GetTenantCredentialAsync(Guid tenantId, string systemType, CancellationToken cancellationToken)
        {
            var credential = await _unitOfWork.TenantCredentials.GetByTenantIdAndSystemTypeAsync(tenantId, systemType, cancellationToken);
            if (credential == null)
            {
                throw new KeyNotFoundException($"Credentials for system type {systemType} not found for tenant {tenantId}.");
            }
            return credential;
        }
    }
}