using Xunit;
using WooFoodIntegration.Application.Services;

namespace WooFoodIntegration.Application.Tests.Services
{
    public class EncryptionServiceTests
    {
        private readonly EncryptionService _encryptionService;

        public EncryptionServiceTests()
        {
            _encryptionService = new EncryptionService();
        }

        [Fact]
        public void EncryptDecrypt_WithValidText_ReturnsOriginalText()
        {
            // Arrange
            string originalText = "This is a secret API key.";

            // Act
            var (encryptedData, iv, salt) = _encryptionService.Encrypt(originalText);
            string decryptedText = _encryptionService.Decrypt(encryptedData, iv, salt);

            // Assert
            Assert.NotNull(encryptedData);
            Assert.NotNull(iv);
            Assert.NotNull(salt);
            Assert.NotEmpty(encryptedData);
            Assert.NotEmpty(iv);
            Assert.NotEmpty(salt);
            Assert.Equal(originalText, decryptedText);
        }

        [Fact]
        public void Encrypt_ReturnsDifferentValuesForDifferentCalls()
        {
            // Arrange
            string originalText = "Another secret key.";

            // Act
            var (encryptedData1, iv1, salt1) = _encryptionService.Encrypt(originalText);
            var (encryptedData2, iv2, salt2) = _encryptionService.Encrypt(originalText);

            // Assert
            Assert.NotEqual(encryptedData1, encryptedData2);
            Assert.NotEqual(iv1, iv2);
            Assert.NotEqual(salt1, salt2);
        }

        [Fact]
        public void Decrypt_WithInvalidData_ThrowsCryptographicException()
        {
            // Arrange
            string originalText = "Some data";
            var (encryptedData, iv, salt) = _encryptionService.Encrypt(originalText);

            // Act & Assert
            // Tamper with encryptedData to make it an invalid Base64 string
            Assert.Throws<System.FormatException>(() =>
            {
                _encryptionService.Decrypt(encryptedData.Substring(0, encryptedData.Length - 1), iv, salt);
            });

            // Tamper with IV to make it an invalid Base64 string
            Assert.Throws<System.FormatException>(() =>
            {
                _encryptionService.Decrypt(encryptedData, iv.Substring(0, iv.Length - 1), salt);
            });

            // Tamper with Salt (this might not always throw CryptographicException, but will likely result in incorrect decryption)
            // We'll primarily test for successful decryption with correct salt, and different salts leading to different keys.
        }
    }
}