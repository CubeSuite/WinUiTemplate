using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;
using WinUiTemplate.Services.Interfaces;
using WinUiTemplate.Stores.Interfaces;

namespace WinUiTemplate.Services
{
    public class EncryptionService : IEncryptionService {
        // Services & Stores
        private readonly IProgramData programData;

        // Members
        private const int keySizeBytes = 32;
        private const int ivSizeBytes = 12;
        private const int tagSizeBytes = 16;
        private byte[]? key;

        // Constructors

        public EncryptionService(IServiceProvider serviceProvider) {
            programData = serviceProvider.GetRequiredService<IProgramData>();
        }

        // Public Functions

        public async Task<byte[]> EncryptAsync(byte[] plainBytes) {
            return await DoEncryption(plainBytes);
        }

        public async Task<byte[]> DecryptAsync(byte[] cipherBytes) {
            return await DoDecryption(cipherBytes);
        }

        public async Task<string> EncryptToBase64Async(string plainText) {
            if (string.IsNullOrEmpty(plainText)) return "";

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] blob = await DoEncryption(plainBytes);
            return Convert.ToBase64String(blob);
        }

        public async Task<string> DecryptFromBase64Async(string base64CipherText) {
            if (string.IsNullOrEmpty(base64CipherText)) return "";

            byte[] blob = Convert.FromBase64String(base64CipherText);
            byte[] plainBytes = await DoDecryption(blob);
            return Encoding.UTF8.GetString(plainBytes);
        }

        // Private Functions

        private async Task<byte[]> ProtectKeyAsync(byte[] rawKey) {
            DataProtectionProvider provider = new DataProtectionProvider("LOCAL=user");

            IBuffer input = CryptographicBuffer.CreateFromByteArray(rawKey);
            IBuffer protectedBuffer = await provider.ProtectAsync(input);

            CryptographicBuffer.CopyToByteArray(protectedBuffer, out byte[] protectedBytes);
            return protectedBytes;
        }

        private async Task<byte[]> UnprotectKeyAsync(byte[] protectedKey) {
            DataProtectionProvider provider = new DataProtectionProvider();

            IBuffer input = CryptographicBuffer.CreateFromByteArray(protectedKey);
            IBuffer unprotectedBuffer = await provider.UnprotectAsync(input);

            CryptographicBuffer.CopyToByteArray(unprotectedBuffer, out byte[] rawKey);
            return rawKey;
        }

        private async Task<byte[]> GetOrCreateKey() {
            if (key != null) return key;

            if (File.Exists(programData.FilePaths.KeyFile)) {
                byte[] protectedKey = File.ReadAllBytes(programData.FilePaths.KeyFile);
                key = await UnprotectKeyAsync(protectedKey);
                if (key.Length != keySizeBytes) {
                    throw new CryptographicException("Invalid key length.");
                }
            }
            else {
                key = new byte[keySizeBytes];
                RandomNumberGenerator.Fill(key);

                byte[] protectedKey = await ProtectKeyAsync(key);
                File.WriteAllBytes(programData.FilePaths.KeyFile, protectedKey);
            }
            
            return key;
        }

        private async Task<byte[]> DoEncryption(byte[] plainBytes) {
            byte[] key = await GetOrCreateKey();
            byte[] iv = new byte[ivSizeBytes];
            RandomNumberGenerator.Fill(iv);

            byte[] tag = new byte[tagSizeBytes];
            byte[] cipher = new byte[plainBytes.Length];

            using (AesGcm aes = new AesGcm(key, tagSizeBytes)) {
                aes.Encrypt(iv, plainBytes, cipher, tag);
            }

            byte[] result = new byte[ivSizeBytes + tagSizeBytes + cipher.Length];
            Array.Copy(iv, 0, result, 0, ivSizeBytes);
            Array.Copy(tag, 0, result, ivSizeBytes, tagSizeBytes);
            Array.Copy(cipher, 0, result, ivSizeBytes + tagSizeBytes, cipher.Length);

            return result;
        }
                
        private async Task<byte[]> DoDecryption(byte[] blob) {
            byte[] key = await GetOrCreateKey();
            byte[] iv = new byte[ivSizeBytes];
            byte[] tag = new byte[tagSizeBytes];
            byte[] cipher = new byte[blob.Length - ivSizeBytes - tagSizeBytes];

            Array.Copy(blob, 0, iv, 0, ivSizeBytes);
            Array.Copy(blob, ivSizeBytes, tag, 0, tagSizeBytes);
            Array.Copy(blob, ivSizeBytes + tagSizeBytes, cipher, 0, cipher.Length);

            byte[] plain = new byte[cipher.Length];

            using (AesGcm aes = new AesGcm(key, tagSizeBytes)) {
                aes.Decrypt(iv, cipher, tag, plain);
            }

            return plain;
        }
    }
}
