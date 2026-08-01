namespace WooFoodIntegration.Application.Interfaces
{
    public interface IEncryptionService
    {
        (string encryptedData, string iv, string salt) Encrypt(string plainText);
        string Decrypt(string encryptedData, string iv, string salt);
    }
}
