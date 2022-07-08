using System.Threading.Tasks;

namespace Compressing.Core
{
    public interface ICompressor
    {
        public byte[] CompressBytesToBytes(byte[] bytes);
        public byte[] CompressFileToBytes(string filePath,bool deleteSourceFile = false);
        public void CompressBytesToFile(byte[] bytes, string compressedFilePath);
        public string CompressFileToFile(string filePath, bool deleteSourceFile = false);
        public void CompressFileToFile(string filePath, string compressedFilePath, bool deleteSourceFile = false);
        
        public Task<byte[]> CompressBytesToBytesAsync(byte[] bytes);
        
        public Task CompressBytesToFileAsync(byte[] bytes, string compressedFilePath);
        
        public Task<string> CompressFileToFileAsync(string filePath, bool deleteSourceFile = false);
        public Task CompressFileToFileAsync(string filePath, string compressedFilePath, bool deleteSourceFile = false); 
        public Task<byte[]> CompressFileToBytesAsync(string filePath, bool deleteSourceFile = false);
        
        
    }
}