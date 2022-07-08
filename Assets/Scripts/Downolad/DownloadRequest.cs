using System;
using Download.Core.Utils.Extensions;

namespace Download.Core
{
    public class DownloadRequest
    {
        private string url;
        private string path;
        private string name;
        private bool deleteFileIfExist;

        public string Url => url;

        public string Path => path;
        public string Name => name;
        public bool DeleteFileIfExist => deleteFileIfExist;

        public DownloadRequest(string url, string path,string name, bool deleteFileIfExist = false)
        {
            this.url = url;
            this.path = path;
            this.name = name;
            this.deleteFileIfExist = deleteFileIfExist;
        }
        public DownloadRequest(string url,string name, bool deleteFileIfExist = false)
        {
            this.url = url;
            this.path = String.Empty;
            
            this.name = name;
            this.deleteFileIfExist = deleteFileIfExist;
        }
    }
}