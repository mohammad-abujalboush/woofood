using System.Security.Cryptography;
using System.Text;
using WooFoodIntegration.Application.Interfaces;

namespace WooFoodIntegration.Application.Services
{
    public class EncryptionService : IEncryptionService
    {
        private const int KeySize = 256;
        private const int Iterations = 10000;

        public (string encryptedData, string iv, string salt) Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = KeySize;
                aes.GenerateIV();
                string iv = Convert.ToBase64String(aes.IV);

                var saltBytes = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(saltBytes);
                }
                string salt = Convert.ToBase64String(saltBytes);

                var pbkdf2 = new Rfc2898DeriveBytes("YourSecretPassword", saltBytes, Iterations, HashAlgorithmName.SHA256);
                aes.Key = pbkdf2.GetBytes(KeySize / 8);

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return (Convert.ToBase64String(ms.ToArray()), iv, salt);
                }
            }
        }

        public string Decrypt(string encryptedData, string iv, string salt)
        {
            var aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.IV = Convert.FromBase64String(iv);

            var saltBytes = Convert.FromBase64String(salt);
            var pbkdf2 = new Rfc2898DeriveBytes("YourSecretPassword", saltBytes, Iterations, HashAlgorithmName.SHA256);
            aes.Key = pbkdf2.GetBytes(KeySize / 8);

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(Convert.FromBase64String(encryptedData)))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}