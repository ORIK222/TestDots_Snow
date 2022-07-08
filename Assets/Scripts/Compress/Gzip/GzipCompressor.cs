using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Compressing.Core
{
    public class GzipCompressor : ICompressor
    {
        private string fileExtension = ".gz";

        /// <summary>
        /// Compress bytes to Gzip bytes
        /// </summary>
        /// <param name="bytes">File bytes</param>
        /// <returns>Compressed bytes</returns>
        public byte[] CompressBytesToBytes(byte[] bytes)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(bytes, 0, bytes.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        /// <summary>
        /// Compress bytes to Gzip bytes and save to "compressedFilePath"
        /// </summary>
        /// <param name="bytes">File bytes</param>
        /// <param name="compressedFilePath">Path to save file</param>
        public void CompressBytesToFile(byte[] bytes, string compressedFilePath)
        {
            var compressedBytes = CompressBytesToBytes(bytes);
            WriteBytesToFile(compressedBytes,compressedFilePath);
        }
        /// <summary>
        /// Compress file to ".gz" file
        /// </summary>
        /// <param name="filePath">The path to the file to be compressed</param>
        /// <param name="deleteSourceFile">Delete source file</param>
        public string CompressFileToFile(string filePath,bool deleteSourceFile = false)
        {
            CompressFileToFile(filePath,filePath + fileExtension);
            return filePath + fileExtension;
        }
        /// <summary>
        /// Compress file to ".gz" file and save to "compressedFilePath"
        /// </summary>
        /// <param name="filePath">The path to the file to be compressed</param>
        /// <param name="compressedFilePath">Path to save compressed file</param>
        /// <param name="deleteSourceFile">Delete source file</param>
        public void CompressFileToFile(string filePath, string compressedFilePath,bool deleteSourceFile = false)
        {
            var bytes = GetFileBytes(filePath, deleteSourceFile);
            var compressedBytes = CompressBytesToBytes(bytes);
            WriteBytesToFile(compressedBytes,compressedFilePath);
        }
        /// <summary>
        /// Compress file to Gzip bytes
        /// </summary>
        /// <param name="filePath">Path to file to be compressed</param>
        /// <param name="deleteSourceFile">Delete source file</param>
        /// <returns>Compressed bytes</returns>
        public byte[] CompressFileToBytes(string filePath,bool deleteSourceFile = false)
        {
            var fileBytes = GetFileBytes(filePath, deleteSourceFile);
            return CompressBytesToBytes(fileBytes);
        }
        /// <summary>
        /// Async compress bytes to Gzip bytes
        /// </summary>
        /// <param name="bytes">Bytes to compress</param>
        /// <returns>Compressed bytes</returns>
        public Task<byte[]> CompressBytesToBytesAsync(byte[] bytes)
        {
            var task = new Task<byte[]>(() => CompressBytesToBytes(bytes));
            task.Start();
            return task;
        }
        /// <summary>
        /// Compress bytes to Gzip bytes and save to "compressedFilePath"
        /// </summary>
        /// <param name="bytes">File bytes</param>
        /// <param name="compressedFilePath">Path to save file</param>
        public Task CompressBytesToFileAsync(byte[] bytes, string compressedFilePath)
        {
            var task = new Task(() => CompressBytesToFile(bytes, compressedFilePath));
            task.Start();
            return task;
        }
        /// <summary>
        /// Compress file to ".gz" file and save to "compressedFilePath"
        /// </summary>
        /// <param name="filePath">The path to the file to be compressed</param>
        /// <param name="deleteSourceFile">Delete source file</param>
        public Task<string> CompressFileToFileAsync(string filePath,bool deleteSourceFile = false)
        {
            var task = new Task<string>(() => CompressFileToFile(filePath));
            task.Start();
            return task;
        }
        /// <summary>
        /// Compress file to ".gz" file and save to "compressedFilePath"
        /// </summary>
        /// <param name="filePath">The path to the file to be compressed</param>
        /// <param name="compressedFilePath">Path to save compressed file</param>
        /// <param name="deleteSourceFile">Delete source file</param>
        public Task CompressFileToFileAsync(string filePath, string compressedFilePath,bool deleteSourceFile = false)
        {
            
            
            var task = new Task(() => CompressFileToFile(filePath, compressedFilePath));
            task.Start();
            return task;
        }
        /// <summary>
        /// Compress file bytes to Gzip bytes
        /// </summary>
        /// <param name="filePath">Path to file to be compressed</param>
        /// <param name="deleteSourceFile">Delete source file</param>
        /// <returns>Compressed bytes</returns>
        public Task<byte[]> CompressFileToBytesAsync(string filePath,bool deleteSourceFile = false)
        {
            var task = new Task<byte[]>(() => CompressFileToBytes(filePath, deleteSourceFile));
            task.Start();
            return task;
        }
        
        byte[] GetFileBytes(string filePath, bool deleteFile)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File {filePath} not found");

            var bytes = File.ReadAllBytes(filePath);
            
            if(deleteFile)
                File.Delete(filePath);

            return bytes;
        }

        void WriteBytesToFile(byte[] bytes, string filePath)
        {
            var path = filePath + fileExtension;
            
            if (File.Exists(path))
                throw new FileLoadException($"File on path {filePath} allready have");
            
            File.WriteAllBytes(filePath,bytes);
        }
    }
}