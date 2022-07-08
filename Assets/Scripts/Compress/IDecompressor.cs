using System.Threading.Tasks;

namespace Compressing.Core
{
    public interface IDecompressor
    {
        public byte[] DecompressBytesToBytes(byte[] bytes);
        public byte[] DecompressFileToBytes(string filePath, bool deleteSourceFile = false);
        public void DecompressBytesToFile(byte[] bytes, string decompressFilePath);
        public string DecompressFileToFile(string filePath, bool deleteSourceFile = false, Cancellator cancellator = null);
        public void DecompressFileToFile(string filePath, string decompressFilePath, bool deleteSourceFile = false, Cancellator cancellator = null);

        public Task<byte[]> DecompressBytesToBytesAsync(byte[] bytes);
        public Task DecompressBytesToFileAsync(byte[] bytes, string decompressedFilePath);
        public Task<string> DecompressFileToFileAsync(string filePath, bool deleteSourceFile = false, Cancellator cancellator = null);

        public Task DecompressFileToFileAsync(string filePath, string decompressedFilePath, bool deleteSourceFile = false, Cancellator cancellator = null);
        public Task<byte[]> DecompressFileToBytesAsync(string filePath, bool deleteSourceFile = false);
    }
}