using System.Collections.Generic;

namespace _Core.Scripts.Downolad
{
    public static class DownloadedObjectsMaster
    {
        private static bool initialized = false;

        private static List<DownloadedObjectData> downloadedObjects;

        static void Initialize()
        {
            downloadedObjects = PersistentCache.TryLoad<List<DownloadedObjectData>>("DownloadedObjects");
            if (downloadedObjects == null) downloadedObjects = new List<DownloadedObjectData>();
            initialized = true;
        }
        
        
        public static bool IsDownloaded(string url)
        {
            if(!initialized)
                Initialize();

            foreach (var downloadedObject in downloadedObjects)
            {
                if (downloadedObject.url == url) return true;
            }
            return false;
        }
        public static bool IsDownloaded(string url, out string path)
        {
            if(!initialized)
                Initialize();

            foreach (var downloadedObject in downloadedObjects)
            {
                if (downloadedObject.url == url)
                {
                    path = downloadedObject.path;
                    return true;
                }
            }

            path = null;
            return false;
        }

        public static void AddDownloadedObject(string url, string path)
        {
            var downloadedData = new DownloadedObjectData();
            downloadedData.path = path;
            downloadedData.url = url;
            AddDownloadedObject(downloadedData);
        }

        public static void AddDownloadedObject(DownloadedObjectData downloadedData)
        {
            if(!initialized)
                Initialize();
            
            downloadedObjects.Add(downloadedData);
            
            PersistentCache.Save("DownloadedObjects", downloadedObjects);
        }

        public static void ClearData()
        {
            downloadedObjects.Clear();
            PersistentCache.Save("DownloadedObjects", downloadedObjects);
        }

        

    }
}