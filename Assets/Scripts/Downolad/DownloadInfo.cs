namespace Download.Core
{
    public class DownloadInfo
    {
        private DownloadResult result;
        private DownloadError error;
        private string downloadedPath;
        private DownloadedObject downloadedObject;
        private ResponseData responseData;

        public ResponseData ResponseData => responseData;

        public DownloadResult Result => result;
        public DownloadError Error => error;
        public string DownloadedPath => downloadedPath;
        public DownloadedObject DownloadedObjectData => downloadedObject;

        public DownloadInfo(string downloadedPath, ResponseData responseData)
        {
            this.downloadedPath = downloadedPath;
            this.responseData = responseData;
        }

        public DownloadInfo(DownloadResult result, string downloadedPath, ResponseData responseData)
            : this(downloadedPath, responseData)
        {
            this.result = result;
        }

        public DownloadInfo(DownloadResult result, DownloadError error, ResponseData responseData)
        {
            this.result = result;
            this.error = error;
            this.responseData = responseData;
        }
        public DownloadInfo(DownloadResult result, DownloadError error, DownloadedObject downloadedObject, ResponseData responseData)
            : this(result, error, responseData)
        {
            this.downloadedObject = downloadedObject;
        }
    }
    public enum DownloadResult
    {
        Success,
        Fail
    }

    public enum DownloadError
    {
        FileExist,
        ConnectionError,
        Unknown,
        ProtocolError,
        DataProcessingError,
        Forbiden
    }
}