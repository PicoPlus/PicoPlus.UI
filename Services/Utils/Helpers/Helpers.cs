using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace PicoPlus.Services
{
    public class Helpers
    {
        private readonly string _masterKey;

        public Helpers(string masterKey)
        {
            _masterKey = masterKey;
        }

        public static string ConvertToPersianCalendar(DateTime dateTime)
        {
            // Define Tehran time zone
            TimeZoneInfo tehranTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");

            // Convert the provided DateTime object to the Persian calendar format in Tehran time zone
            DateTime tehranDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, tehranTimeZone);
            PersianCalendar persianCalendar = new PersianCalendar();
            int year = persianCalendar.GetYear(tehranDateTime);
            int month = persianCalendar.GetMonth(tehranDateTime);
            int day = persianCalendar.GetDayOfMonth(tehranDateTime);

            // Format the Persian DateTime string
            string persianDateTime = $"{year:D4}/{month:D2}/{day:D2} {tehranDateTime:HH:mm}";

            return persianDateTime;
        }

        public static long GenerateOTP()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] tokenData = new byte[4]; // 4 bytes will be enough for a 5-digit OTP
                rng.GetBytes(tokenData);

                // Convert the random bytes to an integer value
                int otpValue = BitConverter.ToInt32(tokenData, 0);

                // Ensure the OTP value is positive
                otpValue = Math.Abs(otpValue);

                // Modulus to get 5-digit OTP
                otpValue = otpValue % 100000;

                return otpValue;
            }
        }

        // Method to convert model to string array of property names
        public static string[] ConvertModelToStringArray<T>(T model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            List<string> includeParams = new List<string>();
            ConvertPropertiesToStringArray(model, includeParams);

            return includeParams.ToArray();
        }

        // Helper method to recursively convert properties to string array
        private static void ConvertPropertiesToStringArray<T>(T model, List<string> includeParams, string prefix = "")
        {
            PropertyInfo[] properties = model.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                object value = property.GetValue(model);

                if (value != null && !property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
                {
                    ConvertPropertiesToStringArray(value, includeParams, $"{prefix}{property.Name}.");
                }
                else
                {
                    includeParams.Add($"{prefix}{property.Name}");
                }
            }
        }

        // Encrypt the session key with the master key
        public string EncryptSessionKey(string sessionKey)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_masterKey);
                aes.GenerateIV();
                aes.Mode = CipherMode.CBC;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new System.IO.MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(sessionKey);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        // Decrypt the session key with the master key
        public string DecryptSessionKey(string encryptedSessionKey)
        {
            var fullCipher = Convert.FromBase64String(encryptedSessionKey);

            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_masterKey);
                aes.IV = fullCipher.Take(aes.BlockSize / 8).ToArray();
                aes.Mode = CipherMode.CBC;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new System.IO.MemoryStream(fullCipher.Skip(aes.BlockSize / 8).ToArray()))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}
