using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace WinUiTemplate.Tests
{
    internal static class TestUtils
    {
        internal static async Task<StorageFolder> GetTempFolder() {
            string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);
            Directory.CreateDirectory(Path.Combine(tempPath, "subfolder"));

            string testFilePath = Path.Combine(tempPath, "subfolder", "test.txt");
            File.WriteAllText(testFilePath, "test content");

            return await StorageFolder.GetFolderFromPathAsync(tempPath);
        }

        internal static async Task CleanupTempFolder(StorageFolder tempFolder) {
            if (Directory.Exists(tempFolder.Path)) {
                Directory.Delete(tempFolder.Path, true);
            }
        }

        internal static async Task<StorageFile> GetTempZipFile(StorageFolder tempParentFolder) {
            string zipPath = Path.Combine(tempParentFolder.Path, "output.zip");

            using (FileStream fs = new FileStream(zipPath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create)) {
                ZipArchiveEntry entry = archive.CreateEntry("subfolder/test.txt");
                using (StreamWriter writer = new StreamWriter(entry.Open())) {
                    await writer.WriteAsync("test content");
                }
            }

            StorageFile zipFile = await StorageFile.GetFileFromPathAsync(zipPath);
            File.Exists(zipPath).Should().BeTrue();
            return zipFile;
        }

        internal static async Task CleanupTempZipFile(StorageFile tempZipFile) {
            if (File.Exists(tempZipFile.Path)) {
                await tempZipFile.DeleteAsync();
            }
        }
    }
}
