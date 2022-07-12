using System.Security.Cryptography;
using System.Text;

namespace PoCSteganography.Helpers
{
    public static class AESEncryptionHelper
    {
        public static string Encrypt(string clearMessage, string password)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.
            byte[] saltStringBytes = Generate128BitsOfRandomEntropy();
            byte[] ivStringBytes = Generate128BitsOfRandomEntropy();
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(clearMessage);
            using (Rfc2898DeriveBytes derivedPassword = new(password, saltStringBytes, 1000))
            {
                byte[] keyBytes = derivedPassword.GetBytes(32); //256bits
                using (Aes symmetricKey = Aes.Create())
                {
                    symmetricKey.BlockSize = 128;
                    using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (MemoryStream memoryStream = new())
                        {
                            using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                byte[] cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();

                                return Convert.ToBase64String(cipherTextBytes);
                            }
                        }
                    }
                }
            }
        }

        public static string Decrypt(string encryptedMessage, string password)
        {
            // Get the complete stream of bytes that represent:
            // [16 bytes of Salt] + [16 bytes of IV] + [n bytes of CipherText]
            byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(encryptedMessage);
            // Get the saltbytes by extracting the first 16 bytes from the supplied cipherText bytes.
            byte[] saltStringBytes = cipherTextBytesWithSaltAndIv.Take(16).ToArray();
            // Get the IV bytes by extracting the next 16 bytes from the supplied cipherText bytes.
            byte[] ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(16).Take(16).ToArray();
            // Get the actual cipher text bytes by removing the first 32 bytes from the cipherText string.
            byte[] cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip(32).Take(cipherTextBytesWithSaltAndIv.Length - 32).ToArray();

            using (Rfc2898DeriveBytes derivedPassword = new(password, saltStringBytes, 1000))
            {
                byte[] keyBytes = derivedPassword.GetBytes(32); //256bits
                using (Aes symmetricKey = Aes.Create())
                {
                    symmetricKey.BlockSize = 128;
                    using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                    {
                        using (MemoryStream memoryStream = new(cipherTextBytes))
                        {
                            using (CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                using (StreamReader plainTextReader = new(cryptoStream))
                                {
                                    return plainTextReader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static byte[] Generate128BitsOfRandomEntropy()
        {
            byte[] randomBytes = new byte[16]; // 16 Bytes will give us 128 bits.

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                // Fill the array with cryptographically secure random bytes.
                rng.GetBytes(randomBytes);
            }

            return randomBytes;
        }
    }
}
