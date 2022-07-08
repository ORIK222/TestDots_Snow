using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Compressing.Core
{
    public class GzipDecompressor : IDecompressor
    {
        /// <summary>
        /// Decompress gzip bytes array to normal bytes array
        /// </summary>
        /// <param name="bytes">Gzip bytes array</param>
        /// <returns>Normal bytes array</returns>
        
        public byte[] DecompressBytesToBytes(byte[] bytes)
        {
           // CheckBytesCompress(bytes);
            
           using (var compressedStream = new MemoryStream(bytes))
           using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
           using (var resultStream = new MemoryStream())
           {
               zipStream.CopyTo(resultStream);
               return resultStream.ToArray();
           }
        }

        /// <summary>
        /// Read file bytes and decompress to normal bytes array
        /// </summary>
        /// <param name="filePath">Path to compressed file</param>
        /// <param name="deleteSourceFile">Delete compressed file</param>
        /// <returns>Normal bytes array</returns>
        public byte[] DecompressFileToBytes(string filePath, bool deleteSourceFile = false)
        {
            var fileBytes = GetFileBytes(filePath, deleteSourceFile);
            return DecompressBytesToBytes(fileBytes);
        }

        
        /// <summary>
        /// Decompress compressed bytes array to normal and save to "decompressedFilePath" path
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="decompressFilePath"></param>
        public void DecompressBytesToFile(byte[] bytes, string decompressFilePath)
        {
            var decompressedBytes = DecompressBytesToBytes(bytes);
            WriteBytesToFile(decompressedBytes,decompressFilePath);
        }



        /// <summary>
        /// Decompress file bytes array to normal bytes array and save to file
        /// </summary>
        /// <param name="filePath">Path to compressed file</param>
        /// <param name="deleteSourceFile">Delete compressed file</param>
        /// <param name="cancellator">The cancellator.</param>
        /// <returns>
        /// Decompressed file path
        /// </returns>
        public string DecompressFileToFile(string filePath, bool deleteSourceFile = false, Cancellator cancellator = null)
        {
            var newPath = filePath.Replace(".gz", "");
            DecompressFileToFile(filePath, newPath, deleteSourceFile, cancellator);
            return newPath;
        }

        /// <summary>
        /// Decompress file bytes array to normal bytes array and save to "decompressedFilePath"
        /// </summary>
        /// <param name="filePath">Path to compressed file</param>
        /// <param name="decompressFilePath">Path to save decompressed file</param>
        /// <param name="deleteSourceFile">Delete compressed file</param>
        /// <param name="cancellator">The cancellator.</param>
        public void DecompressFileToFile(string filePath, string decompressFilePath, bool deleteSourceFile = false, Cancellator cancellator = null)
        {
            if (cancellator != null && cancellator.IsCancelled)
            {
                throw new TaskCanceledException();
            }

            var compressedBytes = GetFileBytes(filePath, deleteSourceFile);
            if (cancellator != null && cancellator.IsCancelled)
            {
                throw new TaskCanceledException();
            }

            var decompressedBytes = DecompressBytesToBytes(compressedBytes);
            if (cancellator != null && cancellator.IsCancelled)
            {
                throw new TaskCanceledException();
            }

            WriteBytesToFile(decompressedBytes,decompressFilePath);
        }

        /// <summary>
        /// Async decompress bytes array to normal bytes array
        /// </summary>
        /// <param name="bytes">Compressed bytes</param>
        /// <returns>Decompressed bytes</returns>
        public Task<byte[]> DecompressBytesToBytesAsync(byte[] bytes)
        {
            var task = new Task<byte[]>(() => DecompressBytesToBytes(bytes));
            task.Start();
            return task;
        }

        /// <summary>
        /// Decompress bytes array to normal bytes array and save to "decompressedFilePath"
        /// </summary>
        /// <param name="bytes">Compressed bytes</param>
        /// <param name="decompressedFilePath">Path to save decompressed file</param>
        public Task DecompressBytesToFileAsync(byte[] bytes, string decompressedFilePath)
        {
            var task = new Task(() => DecompressBytesToFile(bytes, decompressedFilePath));
            task.Start();
            return task;
        }

        /// <summary>
        /// Decompress compressed file to normal file
        /// </summary>
        /// <param name="filePath">Path to compressed file</param>
        /// <param name="deleteSourceFile">Delete compressed file</param>
        /// <param name="cancellator">The process cancellator.</param>
        /// <returns>
        /// Path to decompressed file
        /// </returns>
        public Task<string> DecompressFileToFileAsync(string filePath, bool deleteSourceFile = false, Cancellator cancellator = null)
        {
            return Task.Factory.StartNew(() => DecompressFileToFile(filePath, deleteSourceFile, cancellator));
        }

        /// <summary>
        /// Decompress compressed file to normal file and save to "decompressedFilePath"
        /// </summary>
        /// <param name="filePath">Path to compressed file</param>
        /// <param name="decompressedFilePath">Path to save decompressed file</param>
        /// <param name="deleteSourceFile">Delete compressed file</param>
        /// <param name="cancellator">The process cancellator.</param>
        /// <returns></returns>
        public Task DecompressFileToFileAsync(string filePath, string decompressedFilePath, bool deleteSourceFile = false, Cancellator cancellator = null)
        {
            return Task.Factory.StartNew(() => DecompressFileToFile(filePath, decompressedFilePath, deleteSourceFile, cancellator));
        }

        /// <summary>
        /// Decompress file bytes to normal bytes
        /// </summary>
        /// <param name="filePath">Path to compressed file</param>
        /// <param name="deleteSourceFile">Delete compressed file</param>
        /// <returns>Decompressed bytes array</returns>
        public Task<byte[]> DecompressFileToBytesAsync(string filePath, bool deleteSourceFile = false)
        {
            var task = new Task<byte[]>(() => DecompressFileToBytes(filePath, deleteSourceFile));
            task.Start();
            return task;
        }

        byte[] GetFileBytes(string filePath, bool deleteFile)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File {filePath} not found");

            var bytes =  File.ReadAllBytes(filePath);
            if(deleteFile)
                File.Delete(filePath);
            return bytes;
        }
        void WriteBytesToFile(byte[] bytes, string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            File.WriteAllBytes(filePath,bytes);
        }
        void CheckBytesCompress(byte[] bytes)
        {
            if (!IsGzip(bytes))
                throw new Exception("Its not a '.gz' or '.gzip' compressed bytes");
        }
        void CheckFileCompress(string filePath)
        {
            if (!IsGzip(filePath))
                throw new Exception("Its not a '.gz' or '.gzip' compressed file");
        }
        bool IsGzip(string filePath)
        {
            var bytes = GetFileBytes(filePath, false);
            return IsGzip(bytes);
        }
        bool IsGzip(byte[] bytes)
        {
            return bytes.Length >= 2 &&
                   bytes[0] == 31 &&
                   bytes[1] == 139;
        }
        
    }
}