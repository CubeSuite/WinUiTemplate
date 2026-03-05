using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Services.Interfaces
{
    /// <summary>
    /// Provides functionality for encrypting and decrypting data.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts a byte array asynchronously.
        /// </summary>
        /// <param name="plainBytes">The plain bytes to encrypt.</param>
        /// <returns>The encrypted bytes.</returns>
        Task<byte[]> EncryptAsync(byte[] plainBytes);

        /// <summary>
        /// Decrypts a byte array asynchronously.
        /// </summary>
        /// <param name="cipherBytes">The encrypted bytes to decrypt.</param>
        /// <returns>The decrypted plain bytes.</returns>
        Task<byte[]> DecryptAsync(byte[] cipherBytes);

        /// <summary>
        /// Encrypts a string and returns the result as a Base64-encoded string asynchronously.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <returns>The Base64-encoded encrypted string.</returns>
        Task<string> EncryptToBase64Async(string plainText);

        /// <summary>
        /// Decrypts a Base64-encoded encrypted string asynchronously.
        /// </summary>
        /// <param name="base64CipherText">The Base64-encoded encrypted string to decrypt.</param>
        /// <returns>The decrypted plain text.</returns>
        Task<string> DecryptFromBase64Async(string base64CipherText);
    }
}
