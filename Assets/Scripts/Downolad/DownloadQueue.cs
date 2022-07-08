using Download.Core;
using UnityEngine.Events;

namespace _Core.Scripts.Downolad
{
    public class DownloadQueue
    {
        public DownloadRequest downloadRequest;
        public UnityAction<DownloadInfo> OnSuccess;
        public UnityAction<string> OnFail;
        public Cancellator cancellator;
    }
}