using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LinhKienDienTu_Web.Helpers
{
    public static class EncryptionHelper
    {
        private static string GetKey()
        {
            var key = Environment.GetEnvironmentVariable("SGF_ENCRYPTION_KEY");
            if (string.IsNullOrEmpty(key))
            {
                // Fallback for development if not set, ensured 32 bytes for AES-256
                return "SGF_32_BYTE_SECURE_KEY_FOR_AES_256_!!"; 
            }
            return key;
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) return plainText;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(GetKey().PadRight(32).Substring(0, 32));
                    aes.GenerateIV();
                    byte[] iv = aes.IV;

                    using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    using (var msData = new MemoryStream())
                    {
                        msData.Write(iv, 0, iv.Length);
                        using (var cs = new CryptoStream(msData, encryptor, CryptoStreamMode.Write))
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(msData.ToArray());
                    }
                }
            }
            catch
            {
                return plainText;
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText)) return cipherText;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);
                if (fullCipher.Length < 16) return cipherText;

                byte[] iv = new byte[16];
                byte[] cipher = new byte[fullCipher.Length - 16];

                Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
                Buffer.BlockCopy(fullCipher, 16, cipher, 0, fullCipher.Length - 16);

                using (var aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(GetKey().PadRight(32).Substring(0, 32));
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var msCipher = new MemoryStream(cipher))
                    using (var cs = new CryptoStream(msCipher, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                return cipherText; // Trả về text gốc nếu không thể giải mã (dữ liệu cũ chưa mã hóa)
            }
        }
    }
}
