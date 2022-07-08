using UnityEngine.Networking;

namespace _Core.Scripts.Downolad
{
    public class RequestData
    {
        private UnityWebRequest request;
        private string targetPath;

        public UnityWebRequest Request => request;
        public string TargetPath => targetPath;

        public RequestData(UnityWebRequest request, string targetPath = null)
        {
            this.request = request;
            this.targetPath = targetPath;
        }
    }
}