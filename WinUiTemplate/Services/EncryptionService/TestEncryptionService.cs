using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using WinUiTemplate.Services.Interfaces;

namespace WinUiTemplate.Services.Testing
{
    public class TestEncryptionService : IEncryptionService
    {
        // Services & Stores
        //private readonly ILoggerService logger;

        // Constructors

        public TestEncryptionService(IServiceProvider serviceProvider) {
            //logger = serviceProvider.GetRequiredService<ILoggerService>();
        }

        // Public Functions
        public Task<byte[]> EncryptAsync(byte[] plainBytes) {
            //logger.LogWarning($"TestEncryptionService: EncryptAsync called with {plainBytes?.Length ?? 0} bytes. Intentionally failing.");
            throw new InvalidOperationException("TestEncryptionService: Intentional failure in EncryptAsync");
        }

        public Task<byte[]> DecryptAsync(byte[] cipherBytes) {
            //logger.LogWarning($"TestEncryptionService: DecryptAsync called with {cipherBytes?.Length ?? 0} bytes. Intentionally failing.");
            throw new InvalidOperationException("TestEncryptionService: Intentional failure in DecryptAsync");
        }

        public Task<string> EncryptToBase64Async(string plainText) {
            //logger.LogWarning($"TestEncryptionService: EncryptToBase64Async called with plainText length={plainText?.Length ?? 0}. Intentionally failing.");
            throw new InvalidOperationException("TestEncryptionService: Intentional failure in EncryptToBase64Async");
        }

        public Task<string> DecryptFromBase64Async(string base64CipherText) {
            //logger.LogWarning($"TestEncryptionService: DecryptFromBase64Async called with base64CipherText length={base64CipherText?.Length ?? 0}. Intentionally failing.");
            throw new InvalidOperationException("TestEncryptionService: Intentional failure in DecryptFromBase64Async");
        }
    }
}
