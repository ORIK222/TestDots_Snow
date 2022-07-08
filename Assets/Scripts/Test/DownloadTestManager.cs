using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DownloadTestManager : MonoBehaviour
{
    [SerializeField] private string url;
    
    // Start is called before the first frame update
    void Start()
    {
        RequestHandler requestGet = new RequestHandler();
        var path = Path.Combine(Application.dataPath, "TestDownload" + Path.GetExtension(url));
        if(File.Exists(path))
            File.Delete(path);
        StartCoroutine(requestGet.SendGetRequestAsync(url,path, null,
            //StartCoroutine(StartDownload(downloadQueueObject.downloadRequest.Url,
            (ResponseData rd) =>
            {
                if (rd.success)
                {
                    //Success download or get
                    //После уачной загрузки удаляем обьект из очереди и вызывает этот же метод для загрузки следующего обьекта

                    
                    Debug.Log($"Success download {requestGet.Name}");
                    Debug.Log($"Saved to {path}");
                    
                    //File.WriteAllBytes(path, rd.byteData);
                    //downloadQueueObject.OnSuccess?.Invoke(new DownloadInfo(rd.message));
                }
                else
                {
                        Debug.Log($"Error download");
                    Debug.Log(rd.message);
                    // parse errors
                }
            }));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
