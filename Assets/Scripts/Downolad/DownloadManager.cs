using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using _Core.Scripts.Downolad;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Download.Core
{
    public class DownloadManager : MonoBehaviour
    {
        [SerializeField] private bool isDebug = default;
        public static DownloadManager Instance;
        
        private Queue<DownloadQueue> downloadQueue;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else 
                Destroy(this);
            
            DontDestroyOnLoad(gameObject);
        }

        public void AddDownloadQueue(IEnumerable<DownloadQueue> newDownloadQueue)
        {
            foreach (var queue in newDownloadQueue)
                AddDownloadQueue(queue);
        }
        public void AddDownloadQueue(DownloadRequest downloadRequest,UnityAction<DownloadInfo> OnSuccess, UnityAction<string> OnFail)
        {
            var downloadQueueObject = new DownloadQueue()
            {
                downloadRequest = downloadRequest,
                OnSuccess = OnSuccess,
                OnFail = OnFail
            };
            AddDownloadQueue(downloadQueueObject);
        }
        
        public IConnection AddDownloadQueue(DownloadQueue downloadQueueObject)
        {
            //TODO перед загрузкой добавить проверку на то не загружен ли файл
            // if (DownloadedObjectsMaster.IsDownloaded(downloadQueueObject.downloadRequest.Url, out string path))
            // {
            //     if (isDebug)
            //         Debug.Log($"File allready downloaded to path- {path}");
            //     var downloadInfo = new DownloadInfo(path);
            //     downloadQueueObject.OnSuccess?.Invoke(downloadInfo);
            //     return;
            // }
            
            if(isDebug)
                Debug.Log($"Add download queue {downloadQueueObject.downloadRequest.Name} on url {downloadQueueObject.downloadRequest.Url} to path {downloadQueueObject.downloadRequest.Name}");

            if (string.IsNullOrEmpty(downloadQueueObject.downloadRequest.Url))
            {
                throw new Exception("Can't add to download queue because url is null. Name is " + downloadQueueObject.downloadRequest.Name);
            }
            else
            {
                if (!Directory.Exists(Constants.DownloadPath))
                    Directory.CreateDirectory(Constants.DownloadPath);

                downloadQueue ??= new Queue<DownloadQueue>();
                downloadQueue.Enqueue(downloadQueueObject);
            }

            return DownloadNext();
        }



        IEnumerator StartDownload(string url, UnityAction<ResponseData> OnDone)
        {
            var request = new UnityWebRequest(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SendWebRequest();
            
            while (!request.isDone)
                yield return new WaitForSeconds(0.1f);

            ResponseData data = new ResponseData();
            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                    data.success = true;
                    data.message = request.downloadHandler.text;
                    break;
                default:
                    data.success = false;
                    data.message = request.error;
                    break;
            }
            OnDone?.Invoke(data);
        }

        private IConnection DownloadNext()
        {
            IConnection connection = null;
            if (downloadQueue.Count > 0)
            {
                var downloadQueueObject = downloadQueue.Dequeue();
                var path = downloadQueueObject.downloadRequest.Path;
                if (downloadQueueObject.downloadRequest.DeleteFileIfExist && File.Exists(path))
                {
                    File.Delete(path);
                }

                if (isDebug)
                    Debug.Log($"Start download file \n" +
                              $"Name- {downloadQueueObject.downloadRequest.Name} \n" +
                              $"Url- {downloadQueueObject.downloadRequest.Url} \n" +
                              $"TargetPath- {path}");

                RequestHandler requestGet = new RequestHandler();
                requestGet.IsDebug = isDebug;
                connection = requestGet;
                StartCoroutine(requestGet.SendGetRequestAsync(downloadQueueObject.downloadRequest.Url, path, null,
                    (ResponseData rd) =>
                    {
                        if (rd.success)
                        {
                            //Success download or get
                            //После уачной загрузки удаляем обьект из очереди и вызывает этот же метод для загрузки следующего обьекта
                            if (isDebug)
                                Debug.Log($"Success download to path- {path}");

                            var downloadInfo = new DownloadInfo(path, rd);
                            DownloadedObjectsMaster.AddDownloadedObject(downloadQueueObject.downloadRequest.Url, path);
                            downloadQueueObject.OnSuccess?.Invoke(downloadInfo);
                        }
                        else
                        {
                            if (isDebug)
                                Debug.Log($"Error download");
                            //Debug.Log(rd.message);
                            downloadQueueObject.OnFail?.Invoke(rd.message);
                        }
                    }));

                DownloadNext();
            }

            return connection;
        }
    }
}