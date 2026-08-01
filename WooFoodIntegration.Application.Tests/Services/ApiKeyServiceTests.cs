using Xunit;
using Moq;
using WooFoodIntegration.Application.DTOs;
using WooFoodIntegration.Application.Interfaces;
using WooFoodIntegration.Application.Services;
using WooFoodIntegration.Domain.Models;
using WooFoodIntegration.Domain.Repositories;
using System.Threading;
using System.Security.Cryptography;
using System.Text;

namespace WooFoodIntegration.Application.Tests.Services
{
    public class ApiKeyServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IApiKeyRepository> _mockApiKeyRepository;
        private readonly ApiKeyService _apiKeyService;

        public ApiKeyServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockApiKeyRepository = new Mock<IApiKeyRepository>();

            _mockUnitOfWork.Setup(uow => uow.ApiKeys).Returns(_mockApiKeyRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _apiKeyService = new ApiKeyService(_mockUnitOfWork.Object);
        }

        private string HashApiKey(string key)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        [Fact]
        public async Task GenerateApiKeyAsync_CreatesAndReturnsRawKey()
        {
            // Arrange
            var createApiKeyDto = new CreateApiKeyDto
            {
                Label = "Test Key",
                TenantId = Guid.NewGuid(),
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            ApiKey? capturedApiKey = null;
            _mockApiKeyRepository.Setup(repo => repo.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()))
                .Callback<ApiKey, CancellationToken>((key, token) => capturedApiKey = key)
                .Returns(Task.CompletedTask);

            // Act
            var rawKey = await _apiKeyService.GenerateApiKeyAsync(createApiKeyDto, CancellationToken.None);

            // Assert
            Assert.NotNull(rawKey);
            Assert.NotEmpty(rawKey);
            Assert.True(Guid.TryParse(rawKey, out _)); // Raw key should be a GUID format

            _mockApiKeyRepository.Verify(repo => repo.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);

            Assert.NotNull(capturedApiKey);
            Assert.Equal(createApiKeyDto.Label, capturedApiKey.Label);
            Assert.Equal(createApiKeyDto.TenantId, capturedApiKey.TenantId);
            Assert.Equal(HashApiKey(rawKey), capturedApiKey.Key);
        }

        [Fact]
        public async Task GetApiKeyAsync_WithValidKey_ReturnsApiKey()
        {
            // Arrange
            var rawKey = Guid.NewGuid().ToString("N");
            var hashedKey = HashApiKey(rawKey);
            var expectedApiKey = new ApiKey { Id = Guid.NewGuid(), Key = hashedKey, Label = "Valid Key", TenantId = Guid.NewGuid() };

            _mockApiKeyRepository.Setup(repo => repo.GetByKeyHashAsync(hashedKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedApiKey);

            // Act
            var result = await _apiKeyService.GetApiKeyAsync(rawKey, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedApiKey.Id, result.Id);
            Assert.Equal(expectedApiKey.Label, result.Label);
            _mockApiKeyRepository.Verify(repo => repo.GetByKeyHashAsync(hashedKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetApiKeyAsync_WithInvalidKey_ReturnsNull()
        {
            // Arrange
            var invalidRawKey = Guid.NewGuid().ToString("N");
            var hashedKey = HashApiKey(invalidRawKey);

            _mockApiKeyRepository.Setup(repo => repo.GetByKeyHashAsync(hashedKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApiKey?)null);

            // Act
            var result = await _apiKeyService.GetApiKeyAsync(invalidRawKey, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _mockApiKeyRepository.Verify(repo => repo.GetByKeyHashAsync(hashedKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RevokeApiKeyAsync_WithValidKey_ReturnsTrueAndRemovesKey()
        {
            // Arrange
            var rawKey = Guid.NewGuid().ToString("N");
            var hashedKey = HashApiKey(rawKey);
            var apiKeyToRevoke = new ApiKey { Id = Guid.NewGuid(), Key = hashedKey, Label = "Key to Revoke", TenantId = Guid.NewGuid() };

            _mockApiKeyRepository.Setup(repo => repo.GetByKeyHashAsync(hashedKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apiKeyToRevoke);
            _mockApiKeyRepository.Setup(repo => repo.RemoveAsync(apiKeyToRevoke, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _apiKeyService.RevokeApiKeyAsync(rawKey, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockApiKeyRepository.Verify(repo => repo.GetByKeyHashAsync(hashedKey, It.IsAny<CancellationToken>()), Times.Once);
            _mockApiKeyRepository.Verify(repo => repo.RemoveAsync(apiKeyToRevoke, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RevokeApiKeyAsync_WithInvalidKey_ReturnsFalseAndDoesNotRemoveKey()
        {
            // Arrange
            var invalidRawKey = Guid.NewGuid().ToString("N");
            var hashedKey = HashApiKey(invalidRawKey);

            _mockApiKeyRepository.Setup(repo => repo.GetByKeyHashAsync(hashedKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((ApiKey?)null);

            // Act
            var result = await _apiKeyService.RevokeApiKeyAsync(invalidRawKey, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockApiKeyRepository.Verify(repo => repo.GetByKeyHashAsync(hashedKey, It.IsAny<CancellationToken>()), Times.Once);
            _mockApiKeyRepository.Verify(repo => repo.RemoveAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}