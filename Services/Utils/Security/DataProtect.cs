using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace PicoPlus.Services.Utils.Security
{
    public class DataProtect
    {
        private static readonly string EncryptionKey =
            Environment.GetEnvironmentVariable("PICOPLUS_ENCRYPTION_KEY")
            ?? throw new InvalidOperationException("PICOPLUS_ENCRYPTION_KEY is not configured.");

        private static readonly string UserDataDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PicoPlus", "user_data");

        public static byte[] SaveUserData(object data)
        {
            string jsonData = JsonConvert.SerializeObject(data);
            return EncryptStringToBytes_Aes(jsonData, EncryptionKey);
        }

        public static async Task<T?> LoadUserDataAsync<T>(string userId, CancellationToken cancellationToken = default)
        {
            var userDataFilePath = BuildUserDataFilePath(userId);

            if (!File.Exists(userDataFilePath))
            {
                return default;
            }

            await using var stream = new FileStream(
                userDataFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var encryptedData = new byte[stream.Length];
            var read = 0;

            while (read < encryptedData.Length)
            {
                var bytesRead = await stream.ReadAsync(encryptedData.AsMemory(read, encryptedData.Length - read), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                read += bytesRead;
            }

            if (read != encryptedData.Length)
            {
                throw new IOException("Could not read the complete encrypted user data file.");
            }

            string jsonData = DecryptStringFromBytes_Aes(encryptedData, EncryptionKey);
            return JsonConvert.DeserializeObject<T>(jsonData);
        }

        public static async Task SaveUserDataToFileAsync(object data, string userId, CancellationToken cancellationToken = default)
        {
            var userDataFilePath = BuildUserDataFilePath(userId);
            Directory.CreateDirectory(Path.GetDirectoryName(userDataFilePath)!);

            var encryptedData = SaveUserData(data);

            await using var stream = new FileStream(
                userDataFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous);

            await stream.WriteAsync(encryptedData.AsMemory(), cancellationToken);
        }

        private static string BuildUserDataFilePath(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID is required.", nameof(userId));
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                userId = userId.Replace(invalid, '_');
            }

            return Path.Combine(UserDataDirectory, $"{userId}.json");
        }

        private static byte[] EncryptStringToBytes_Aes(string plainText, string key)
        {
            byte[] encrypted;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(key);
                aesAlg.GenerateIV();

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
                    {
                        swEncrypt.Write(plainText);
                    }

                    encrypted = msEncrypt.ToArray();
                }
            }

            return encrypted;
        }

        private static string DecryptStringFromBytes_Aes(byte[] cipherText, string key)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(key);
                byte[] iv = new byte[aesAlg.BlockSize / 8];
                Array.Copy(cipherText, 0, iv, 0, iv.Length);
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
    }
}
