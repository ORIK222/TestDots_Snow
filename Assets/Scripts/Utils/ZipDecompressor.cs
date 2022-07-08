using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Download.Core.Utils
{
    public static class ZipDecompressor
    {
        public static string[] Decompress(string zipFilePath, string unpackFolder, bool deleteZip = false)
        {
            if (!Directory.Exists(unpackFolder))
                Directory.CreateDirectory(unpackFolder);

            var filePaths = new List<string>();
            
            using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Update))
            {
                var entrys = zip.Entries;
                foreach (var entry in entrys)
                {
                    var fileName = entry.Name;
                    var path = Path.Combine(unpackFolder, fileName);

                    
                    
                    if (File.Exists(path))
                        File.Delete(path);

                    entry.ExtractToFile(path);
                    filePaths.Add(path);
                }
                // if (entry != null)
                // {
                //     var tempFile = Path.GetTempFileName();
                //     entry.ExtractToFile(tempFile, true);
                //     var content = File.ReadAllText(tempFile);
                //     Debug.Log(content);
                // }
            }
            if(deleteZip)
                File.Delete(zipFilePath);

            return filePaths.ToArray();
        }
    }
}