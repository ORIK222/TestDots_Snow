using System;

namespace Download.Core
{
    [Serializable]
    public class DownloadedObject
    {
        private string path;
        private string url;
        
        private float size;
        
        private DateTime downloadTime;

        public string Path => path;
        public string URL => url;
        
        public float Size => size;
        
        public DateTime DownloadTime => downloadTime;

        public DownloadedObject(string fileName, string path, string url, float size, DateTime downloadTime)
        {
            this.path = path;
            this.url = url;

            this.size = size;

            this.downloadTime = downloadTime;
        } 
        public DownloadedObject(DownloadRequest request, string path, DateTime downloadTime, float size = 0)
        {
            this.path = path;
            this.url = request.Url;

            this.size = size;

            this.downloadTime = downloadTime;
        } 
    }
}