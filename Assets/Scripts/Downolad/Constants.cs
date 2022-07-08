using System.IO;
using UnityEngine;

namespace Download.Core
{
    public static class Constants
    {
#if UNITY_EDITOR
        public static string RootFolderPath => Directory.GetParent(Application.dataPath).FullName;
#else
        public static string RootFolderPath => Application.persistentDataPath;
#endif
        public static string DownloadFolderName = "Downloads";
        public static string GlbFolderName = "Glb";
        public static string ResourcesPath => Path.Combine(RootFolderPath, "Resources");
        
#if UNITY_EDITOR
            public static string DownloadPath
            {
                    get
                    {
                            Debug.Log("Try get path");
                            return Path.Combine(RootFolderPath, DownloadFolderName);
                            
                    }
            }
#else
        public static string DownloadPath
            {
                    get
                    {
                            Debug.Log("Try get path");
                            return Path.Combine(ResourcesPath, DownloadFolderName);
                            
                    }
            }
#endif
        public static string GlbDownloadPath => Path.Combine(DownloadPath, GlbFolderName);
        

    }
}