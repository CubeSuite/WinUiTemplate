using FluentAssertions;
using Moq;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using WinUiTemplate.Services;
using WinUiTemplate.Stores.Interfaces;
using Xunit;

namespace WinUiTemplate.Tests
{
    public class EncryptionServiceTests : IDisposable
    {
        // Services & Stores
        private readonly Mock<IProgramData> mockProgramData;
        private readonly Mock<IFilePaths> mockFilePaths;
        private readonly Mock<IServiceProvider> mockServiceProvider;
        private readonly EncryptionService encryptionService;
        private readonly StorageFolder tempFolder;
        private readonly string keyFilePath;

        // Constructors

        public EncryptionServiceTests() {
            tempFolder = TestUtils.GetTempFolder().Result;
            keyFilePath = Path.Combine(tempFolder.Path, "test.key");

            mockProgramData = new Mock<IProgramData>();
            mockFilePaths = new Mock<IFilePaths>();
            mockServiceProvider = new Mock<IServiceProvider>();

            mockFilePaths.Setup(x => x.KeyFile).Returns(keyFilePath);
            mockProgramData.Setup(x => x.FilePaths).Returns(mockFilePaths.Object);

            mockServiceProvider
                .Setup(x => x.GetService(typeof(IProgramData)))
                .Returns(mockProgramData.Object);

            encryptionService = new EncryptionService(mockServiceProvider.Object);
        }

        public void Dispose() {
            TestUtils.CleanupTempFolder(tempFolder).Wait();
        }

        // Tests

        #region EncryptAsync and DecryptAsync Tests

        [Fact]
        public async Task EncryptAsync_DecryptAsync_RoundTrip_Success() {
            byte[] plainBytes = Encoding.UTF8.GetBytes("Hello, World!");

            byte[] encrypted = await encryptionService.EncryptAsync(plainBytes);
            byte[] decrypted = await encryptionService.DecryptAsync(encrypted);

            decrypted.Should().Equal(plainBytes);
            Encoding.UTF8.GetString(decrypted).Should().Be("Hello, World!");
        }

        [Fact]
        public async Task EncryptAsync_CreatesKeyFileOnFirstUse() {
            File.Exists(keyFilePath).Should().BeFalse();

            byte[] plainBytes = Encoding.UTF8.GetBytes("Test data");
            await encryptionService.EncryptAsync(plainBytes);

            File.Exists(keyFilePath).Should().BeTrue();
        }

        [Fact]
        public async Task EncryptAsync_ProducesDifferentCiphertextForSamePlaintext() {
            byte[] plainBytes = Encoding.UTF8.GetBytes("Same plaintext");

            byte[] encrypted1 = await encryptionService.EncryptAsync(plainBytes);
            byte[] encrypted2 = await encryptionService.EncryptAsync(plainBytes);

            encrypted1.Should().NotEqual(encrypted2);
        }

        [Fact]
        public async Task EncryptAsync_IncludesIVAndTag() {
            byte[] plainBytes = Encoding.UTF8.GetBytes("Test");

            byte[] encrypted = await encryptionService.EncryptAsync(plainBytes);

            encrypted.Length.Should().BeGreaterThan(plainBytes.Length);
            encrypted.Length.Should().Be(12 + 16 + plainBytes.Length);
        }

        [Fact]
        public async Task DecryptAsync_FailsWithCorruptedData() {
            byte[] plainBytes = Encoding.UTF8.GetBytes("Original data");
            byte[] encrypted = await encryptionService.EncryptAsync(plainBytes);

            encrypted[encrypted.Length - 1] ^= 0xFF;

            Func<Task> act = async () => await encryptionService.DecryptAsync(encrypted);

            await act.Should().ThrowAsync<CryptographicException>();
        }

        [Fact]
        public async Task EncryptAsync_DecryptAsync_HandlesEmptyArray() {
            byte[] plainBytes = Array.Empty<byte>();

            byte[] encrypted = await encryptionService.EncryptAsync(plainBytes);
            byte[] decrypted = await encryptionService.DecryptAsync(encrypted);

            decrypted.Should().BeEmpty();
        }

        [Fact]
        public async Task EncryptAsync_DecryptAsync_HandlesLargeData() {
            byte[] plainBytes = new byte[1024 * 1024];
            new Random().NextBytes(plainBytes);

            byte[] encrypted = await encryptionService.EncryptAsync(plainBytes);
            byte[] decrypted = await encryptionService.DecryptAsync(encrypted);

            decrypted.Should().Equal(plainBytes);
        }

        [Fact]
        public async Task EncryptAsync_ReusesExistingKey() {
            byte[] plainBytes1 = Encoding.UTF8.GetBytes("First message");
            await encryptionService.EncryptAsync(plainBytes1);

            DateTime keyFileModified = File.GetLastWriteTimeUtc(keyFilePath);

            await Task.Delay(100);

            byte[] plainBytes2 = Encoding.UTF8.GetBytes("Second message");
            await encryptionService.EncryptAsync(plainBytes2);

            File.GetLastWriteTimeUtc(keyFilePath).Should().Be(keyFileModified);
        }

        #endregion

        #region EncryptToBase64Async and DecryptFromBase64Async Tests

        [Fact]
        public async Task EncryptToBase64Async_DecryptFromBase64Async_RoundTrip_Success() {
            string plainText = "Hello, World!";

            string encrypted = await encryptionService.EncryptToBase64Async(plainText);
            string decrypted = await encryptionService.DecryptFromBase64Async(encrypted);

            decrypted.Should().Be(plainText);
        }

        [Fact]
        public async Task EncryptToBase64Async_ReturnsEmptyForEmptyString() {
            string result = await encryptionService.EncryptToBase64Async("");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task EncryptToBase64Async_ReturnsEmptyForNull() {
            string result = await encryptionService.EncryptToBase64Async(null);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task DecryptFromBase64Async_ReturnsEmptyForEmptyString() {
            string result = await encryptionService.DecryptFromBase64Async("");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task DecryptFromBase64Async_ReturnsEmptyForNull() {
            string result = await encryptionService.DecryptFromBase64Async(null);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task EncryptToBase64Async_ReturnsValidBase64() {
            string plainText = "Test message";

            string encrypted = await encryptionService.EncryptToBase64Async(plainText);

            Func<byte[]> act = () => Convert.FromBase64String(encrypted);
            act.Should().NotThrow();
        }

        [Fact]
        public async Task EncryptToBase64Async_HandlesSpecialCharacters() {
            string plainText = "Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";

            string encrypted = await encryptionService.EncryptToBase64Async(plainText);
            string decrypted = await encryptionService.DecryptFromBase64Async(encrypted);

            decrypted.Should().Be(plainText);
        }

        [Fact]
        public async Task EncryptToBase64Async_HandlesUnicode() {
            string plainText = "Unicode: 你好世界 🌍🔒";

            string encrypted = await encryptionService.EncryptToBase64Async(plainText);
            string decrypted = await encryptionService.DecryptFromBase64Async(encrypted);

            decrypted.Should().Be(plainText);
        }

        [Fact]
        public async Task EncryptToBase64Async_HandlesMultilineText() {
            string plainText = "Line 1\nLine 2\r\nLine 3";

            string encrypted = await encryptionService.EncryptToBase64Async(plainText);
            string decrypted = await encryptionService.DecryptFromBase64Async(encrypted);

            decrypted.Should().Be(plainText);
        }

        [Fact]
        public async Task DecryptFromBase64Async_FailsWithInvalidBase64() {
            string invalidBase64 = "This is not valid base64!";

            Func<Task> act = async () => await encryptionService.DecryptFromBase64Async(invalidBase64);

            await act.Should().ThrowAsync<FormatException>();
        }

        #endregion

        #region Key Management Tests

        [Fact]
        public async Task EncryptionService_CreatesNewKeyWhenKeyFileDoesNotExist() {
            File.Exists(keyFilePath).Should().BeFalse();

            await encryptionService.EncryptAsync(new byte[] { 1, 2, 3 });

            File.Exists(keyFilePath).Should().BeTrue();
            File.ReadAllBytes(keyFilePath).Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task EncryptionService_LoadsExistingKey() {
            byte[] plainBytes = Encoding.UTF8.GetBytes("Test data");
            byte[] encrypted1 = await encryptionService.EncryptAsync(plainBytes);

            EncryptionService newService = new EncryptionService(mockServiceProvider.Object);
            byte[] decrypted = await newService.DecryptAsync(encrypted1);

            decrypted.Should().Equal(plainBytes);
        }

        [Fact]
        public async Task EncryptionService_ThrowsOnInvalidKeyLength() {
            byte[] plainBytes = Encoding.UTF8.GetBytes("Test");
            await encryptionService.EncryptAsync(plainBytes);

            byte[] protectedKey = File.ReadAllBytes(keyFilePath);

            EncryptionService tempService = new EncryptionService(mockServiceProvider.Object);

            string tempKeyPath = Path.Combine(tempFolder.Path, "invalid.key");
            mockFilePaths.Setup(x => x.KeyFile).Returns(tempKeyPath);

            byte[] invalidKey = new byte[16];
            RandomNumberGenerator.Fill(invalidKey);

            Windows.Security.Cryptography.DataProtection.DataProtectionProvider provider = 
                new Windows.Security.Cryptography.DataProtection.DataProtectionProvider("LOCAL=user");
            Windows.Storage.Streams.IBuffer input = Windows.Security.Cryptography.CryptographicBuffer.CreateFromByteArray(invalidKey);
            Windows.Storage.Streams.IBuffer protectedBuffer = await provider.ProtectAsync(input);
            Windows.Security.Cryptography.CryptographicBuffer.CopyToByteArray(protectedBuffer, out byte[] invalidProtectedKey);

            File.WriteAllBytes(tempKeyPath, invalidProtectedKey);

            EncryptionService newService = new EncryptionService(mockServiceProvider.Object);

            Func<Task> act = async () => await newService.EncryptAsync(new byte[] { 1, 2, 3 });

            await act.Should().ThrowAsync<CryptographicException>()
                .WithMessage("Invalid key length.");
        }

        [Fact]
        public async Task EncryptionService_PersistsKeyAcrossInstances() {
            string plainText = "Data to encrypt";
            string encrypted = await encryptionService.EncryptToBase64Async(plainText);

            EncryptionService newService1 = new EncryptionService(mockServiceProvider.Object);
            string decrypted1 = await newService1.DecryptFromBase64Async(encrypted);

            EncryptionService newService2 = new EncryptionService(mockServiceProvider.Object);
            string decrypted2 = await newService2.DecryptFromBase64Async(encrypted);

            decrypted1.Should().Be(plainText);
            decrypted2.Should().Be(plainText);
        }

        #endregion
    }
}
