using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PicoPlus.Services.Utils.Security
{
    public class DataProtect
    {
        private const string encryptionKey = "t4y812o2qDe4Rs440REID8ppZTA0hYK39ybm1gQNAqk=";
        private static string user_id ;
        private static readonly string userDataFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"PicoPlus","user_data", $"{user_id}.json");

        public static byte[] SaveUserData(object data )
        {
            // Serialize user data to JSON
            string jsonData = JsonConvert.SerializeObject(data);

            // Encrypt the JSON data
            byte[] encryptedData = EncryptStringToBytes_Aes(jsonData, encryptionKey);

            // Save encrypted data to a file
            return encryptedData;
        }

        public static void LoadUserData(object data , string userid)
        {
            user_id = userid;
            if (File.Exists(userDataFilePath))
            {
                // Read encrypted data from file
                byte[] encryptedData = File.ReadAllBytes(userDataFilePath);

                // Decrypt the data
                string jsonData = DecryptStringFromBytes_Aes(encryptedData, encryptionKey);

                // Deserialize JSON to user data object
                
            }
            else
            {
               
            }
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
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    encrypted = msEncrypt.ToArray();
                }
            }

            return encrypted;
        }

        private static string DecryptStringFromBytes_Aes(byte[] cipherText, string key)
        {
            string plaintext = null;

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(key);
                byte[] iv = new byte[aesAlg.BlockSize / 8];
                Array.Copy(cipherText, 0, iv, 0, iv.Length);
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText, iv.Length, cipherText.Length - iv.Length))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }
    }
}
