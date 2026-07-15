using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace NovinCRM.Services.Utils.Security
{
    /// <summary>
    /// Provides data protection (encryption/decryption) for user data using
    /// ASP.NET Core Data Protection, which manages keys automatically and securely.
    ///
    /// The previous implementation used a hardcoded AES key constant — a critical
    /// security vulnerability. This version delegates entirely to the platform's
    /// Data Protection API, which handles key generation, rotation, and storage.
    /// </summary>
    public class DataProtect
    {
        // Data Protection purpose string — changing this invalidates all existing ciphertext.
        private const string Purpose = "NovinCRM.UserData.v1";

        private readonly IDataProtector _protector;

        public DataProtect(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector(Purpose);
        }

        /// <summary>
        /// Serialize <paramref name="data"/> to JSON and encrypt it.
        /// Returns the protected payload as a UTF-8 byte array.
        /// </summary>
        public byte[] SaveUserData(object data)
        {
            string jsonData = JsonConvert.SerializeObject(data);
            string protected_ = _protector.Protect(jsonData);
            return System.Text.Encoding.UTF8.GetBytes(protected_);
        }

        /// <summary>
        /// Decrypt a payload produced by <see cref="SaveUserData"/>.
        /// Returns the raw JSON string, or null if the payload is invalid/tampered.
        /// </summary>
        public string? LoadUserData(byte[] encryptedData)
        {
            try
            {
                string cipherText = System.Text.Encoding.UTF8.GetString(encryptedData);
                return _protector.Unprotect(cipherText);
            }
            catch (Exception)
            {
                // Payload was tampered with, expired, or produced by a different key.
                return null;
            }
        }
    }
}
