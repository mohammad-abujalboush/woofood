using Xunit;
using Moq;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.Application.Services;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;
using System.Threading;
using System.Collections.Generic;

namespace WooFoodIntegration.Application.Tests.Services
{
    public class TenantServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITenantRepository> _mockTenantRepository;
        private readonly Mock<ITenantCredentialRepository> _mockTenantCredentialRepository;
        private readonly Mock<IEncryptionService> _mockEncryptionService;
        private readonly TenantService _tenantService;

        public TenantServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTenantRepository = new Mock<ITenantRepository>();
            _mockTenantCredentialRepository = new Mock<ITenantCredentialRepository>();
            _mockEncryptionService = new Mock<IEncryptionService>();

            _mockUnitOfWork.Setup(uow => uow.Tenants).Returns(_mockTenantRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.TenantCredentials).Returns(_mockTenantCredentialRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Setup EncryptionService mock
            _mockEncryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
                .Returns((string plainText) => (@"encrypted-" + plainText, @"iv-" + plainText, @"salt-" + plainText));
            _mockEncryptionService.Setup(es => es.Decrypt(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string encryptedData, string iv, string salt) => encryptedData.Replace(@"encrypted-", string.Empty));

            _tenantService = new TenantService(_mockUnitOfWork.Object, _mockEncryptionService.Object);
        }

        [Fact]
        public async Task CreateTenantAsync_ValidDto_ReturnsTenantDtoAndPersistsTenant()
        {
            // Arrange
            var createTenantDto = new CreateTenantDto
            {
                Name = "Test Tenant",
                WooCommerceBaseUrl = "https://woo.example.com",
                FoodicsBaseUrl = "https://foodics.example.com"
            };

            Tenant? capturedTenant = null;
            _mockTenantRepository.Setup(repo => repo.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
                .Callback<Tenant, CancellationToken>((tenant, token) => capturedTenant = tenant)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _tenantService.CreateTenantAsync(createTenantDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createTenantDto.Name, result.Name);
            Assert.Equal(createTenantDto.WooCommerceBaseUrl, result.WooCommerceBaseUrl);
            Assert.Equal(createTenantDto.FoodicsBaseUrl, result.FoodicsBaseUrl);

            _mockTenantRepository.Verify(repo => repo.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(capturedTenant);
            Assert.NotEqual(Guid.Empty, capturedTenant.Id);
            Assert.Equal(createTenantDto.Name, capturedTenant.Name);
        }

        [Fact]
        public async Task GetTenantByIdAsync_ExistingTenant_ReturnsTenantDto()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var existingTenant = new Tenant
            {
                Id = tenantId,
                Name = "Existing Tenant",
                WooCommerceBaseUrl = "https://woo.existing.com",
                FoodicsBaseUrl = "https://foodics.existing.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockTenantRepository.Setup(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingTenant);

            // Act
            var result = await _tenantService.GetTenantByIdAsync(tenantId, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tenantId, result.Id);
            Assert.Equal(existingTenant.Name, result.Name);
            _mockTenantRepository.Verify(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetTenantByIdAsync_NonExistingTenant_ReturnsNull()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            _mockTenantRepository.Setup(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);

            // Act
            var result = await _tenantService.GetTenantByIdAsync(tenantId, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _mockTenantRepository.Verify(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateTenantCredentialsAsync_NewCredentials_EncryptsAndPersists()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var existingTenant = new Tenant
            {
                Id = tenantId,
                Name = "Existing Tenant",
                WooCommerceBaseUrl = "https://woo.existing.com",
                FoodicsBaseUrl = "https://foodics.existing.com"
            };

            var createCredentialsDto = new CreateTenantCredentialDto
            {
                TenantId = tenantId,
                SystemType = "WooCommerce",
                ApiKey = "woo_api_key_secret"
            };

            _mockTenantRepository.Setup(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingTenant);
            _mockTenantCredentialRepository.Setup(repo => repo.GetByTenantIdAndSystemTypeAsync(tenantId, createCredentialsDto.SystemType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TenantCredential?)null);

            TenantCredential? capturedCredential = null;
            _mockTenantCredentialRepository.Setup(repo => repo.AddAsync(It.IsAny<TenantCredential>(), It.IsAny<CancellationToken>()))
                .Callback<TenantCredential, CancellationToken>((tc, token) => capturedCredential = tc)
                .Returns(Task.CompletedTask);

            // Act
            await _tenantService.UpdateTenantCredentialsAsync(tenantId, createCredentialsDto, CancellationToken.None);

            // Assert
            _mockTenantRepository.Verify(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            _mockTenantCredentialRepository.Verify(repo => repo.GetByTenantIdAndSystemTypeAsync(tenantId, createCredentialsDto.SystemType, It.IsAny<CancellationToken>()), Times.Once);
            _mockEncryptionService.Verify(es => es.Encrypt(createCredentialsDto.ApiKey), Times.Once);
            _mockTenantCredentialRepository.Verify(repo => repo.AddAsync(It.IsAny<TenantCredential>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(capturedCredential);
            Assert.Equal("encrypted-" + createCredentialsDto.ApiKey, capturedCredential.EncryptedApiKey);
            Assert.Equal("iv-" + createCredentialsDto.ApiKey, capturedCredential.Iv);
            Assert.Equal("salt-" + createCredentialsDto.ApiKey, capturedCredential.Salt);
        }

        [Fact]
        public async Task UpdateTenantCredentialsAsync_ExistingCredentials_UpdatesAndEncrypts()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var existingTenant = new Tenant
            {
                Id = tenantId,
                Name = "Existing Tenant",
                WooCommerceBaseUrl = "https://woo.existing.com",
                FoodicsBaseUrl = "https://foodics.existing.com"
            };

            var existingCredential = new TenantCredential
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SystemType = "WooCommerce",
                EncryptedApiKey = "old-encrypted",
                Iv = "old-iv",
                Salt = "old-salt",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var updateCredentialsDto = new CreateTenantCredentialDto
            {
                TenantId = tenantId,
                SystemType = "WooCommerce",
                ApiKey = "new_woo_api_key_secret"
            };

            _mockTenantRepository.Setup(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingTenant);
            _mockTenantCredentialRepository.Setup(repo => repo.GetByTenantIdAndSystemTypeAsync(tenantId, updateCredentialsDto.SystemType, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingCredential);

            // Act
            await _tenantService.UpdateTenantCredentialsAsync(tenantId, updateCredentialsDto, CancellationToken.None);

            // Assert
            _mockTenantRepository.Verify(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            _mockTenantCredentialRepository.Verify(repo => repo.GetByTenantIdAndSystemTypeAsync(tenantId, updateCredentialsDto.SystemType, It.IsAny<CancellationToken>()), Times.Once);
            _mockEncryptionService.Verify(es => es.Encrypt(updateCredentialsDto.ApiKey), Times.Once);
            _mockTenantCredentialRepository.Verify(repo => repo.UpdateAsync(existingCredential, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal("encrypted-" + updateCredentialsDto.ApiKey, existingCredential.EncryptedApiKey);
            Assert.Equal("iv-" + updateCredentialsDto.ApiKey, existingCredential.Iv);
            Assert.Equal("salt-" + updateCredentialsDto.ApiKey, existingCredential.Salt);
            Assert.True(existingCredential.UpdatedAt > existingCredential.CreatedAt);
        }

        [Fact]
        public async Task UpdateTenantCredentialsAsync_TenantNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var createCredentialsDto = new CreateTenantCredentialDto
            {
                TenantId = tenantId,
                SystemType = "WooCommerce",
                ApiKey = "woo_api_key_secret"
            };

            _mockTenantRepository.Setup(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tenant?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _tenantService.UpdateTenantCredentialsAsync(tenantId, createCredentialsDto, CancellationToken.None));

            _mockTenantRepository.Verify(repo => repo.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
            _mockTenantCredentialRepository.Verify(repo => repo.GetByTenantIdAndSystemTypeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockEncryptionService.Verify(es => es.Encrypt(It.IsAny<string>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetTenantCredentialAsync_ExistingCredential_ReturnsCredential()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var systemType = "Foodics";
            var expectedCredential = new TenantCredential
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SystemType = systemType,
                EncryptedApiKey = "encrypted-foodics",
                Iv = "iv-foodics",
                Salt = "salt-foodics",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockTenantCredentialRepository.Setup(repo => repo.GetByTenantIdAndSystemTypeAsync(tenantId, systemType, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedCredential);

            // Act
            var result = await _tenantService.GetTenantCredentialAsync(tenantId, systemType, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedCredential.Id, result.Id);
            Assert.Equal(expectedCredential.EncryptedApiKey, result.EncryptedApiKey);
            _mockTenantCredentialRepository.Verify(repo => repo.GetByTenantIdAndSystemTypeAsync(tenantId, systemType, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetTenantCredentialAsync_NonExistingCredential_ThrowsKeyNotFoundException()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var systemType = "Foodics";

            _mockTenantCredentialRepository.Setup(repo => repo.GetByTenantIdAndSystemTypeAsync(tenantId, systemType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((TenantCredential?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _tenantService.GetTenantCredentialAsync(tenantId, systemType, CancellationToken.None));

            _mockTenantCredentialRepository.Verify(repo => repo.GetByTenantIdAndSystemTypeAsync(tenantId, systemType, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}