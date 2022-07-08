using System;
using System.Collections;
using System.Collections.Generic;
using _Core.Scripts.Downolad;
using Download.Core;
using UnityEngine;
using UnityEngine.Networking;

public class RequestHandler : IConnection
{
    private bool isCompleted;
    private bool isSuccess;
    private bool isAborted;
    private string error;
    private string message;
    private ResponseData responseData;
    private UnityWebRequest currentRequest;
    private DownloadHandlerFileCustom downloadHandler;

    public string Name { get; set; } = string.Empty;
    public bool IsDebug { get; set; } = false;
    
    public bool IsAborted
    {
        get
        {
            lock (this)
            {
                return this.isAborted;
            }
        }
    }

    public bool IsCompleted
    {
        get
        {
            lock (this)
            {
                return this.isCompleted;
            }
        }
    }

    public string Error
    {
        get
        {
            lock (this)
            {
                return this.error;
            }
        }
    }

    public ResponseData ResponseData
    {
        get
        {
            lock (this)
            {
                return this.responseData;
            }
        }
    }

    public bool IsSuccess
    {
        get
        {
            lock (this)
            {
                return this.isSuccess;
            }
        }
    }

    public string Message
    {
        get
        {
            lock (this)
            {
                return this.message;
            }
        }
    }


    public IEnumerator SendPostRequestAsync(string url, WWWForm form, IDictionary<string, string> requestHeaders, Action<ResponseData> responseAction)
    {
        return SendRequestAsync(new RequestData(UnityWebRequest.Post(url, form)), requestHeaders, responseAction);
    }

    public IEnumerator SendPutRequestAsync(string url, string json, IDictionary<string,string> requestHeaders, Action<ResponseData> responseAction)
    {
        return SendRequestAsync(new RequestData(UnityWebRequest.Put(url, json)), requestHeaders, responseAction);
    }

    public IEnumerator SendGetRequestAsync(string url, IDictionary<string, string> requestHeaders, Action<ResponseData> responseAction)
    {
        return SendRequestAsync(new RequestData(UnityWebRequest.Get(url)), requestHeaders, responseAction);
    }
    public IEnumerator SendGetRequestAsync(string url, string targetPath, IDictionary<string, string> requestHeaders, Action<ResponseData> responseAction)
    {
        return SendRequestAsync(new RequestData(UnityWebRequest.Get(url),targetPath), requestHeaders, responseAction);
    }
    
   
    private void ProcessResponse(UnityWebRequest request, Action<ResponseData> action)
    {
        ResponseData rd = null;
        try
        {
            bool error = request.result != UnityWebRequest.Result.Success;
            if (error && request.downloadHandler.text.ToLower().Contains("not found"))
            {
                rd = new ResponseData();
                rd.success = true;
                rd.message = "{}";
            }
            else if (!error)
            {
                try
                {
                    string binaryData = request.downloadHandler.text;
                    byte[] byteData = request.downloadHandler.data;
                    rd = new ResponseData();
                    rd.success = true;
                    rd.message = binaryData;
                    rd.byteData = byteData;
                }
                catch (Exception exc)
                {
                    // silently ignore.
                    Debug.Log(exc);
                }
            }

            if (rd == null)
            {
                rd = new ResponseData();
                rd.success = false;
                rd.message = request.error;
            }
        }
        finally
        {
            lock (this)
            {
                this.responseData = rd;
                this.isCompleted = true;
                this.isSuccess = rd.success;
                if (this.isSuccess)
                {
                    this.message = rd.message;
                    this.error = null;
                }
                else
                {
                    this.message = null;
                    this.error = rd.message;
                }
                this.currentRequest = null;
            }

            if (downloadHandler != null)
            {
                downloadHandler.Dispose();
                downloadHandler = null;
            }

            action(responseData);
        }
    }

    private IEnumerator SendRequestAsync(RequestData requestData, IDictionary<string,string> requestHeaders, Action<ResponseData> responseAction)
    {
        try
        {
            if (!string.IsNullOrEmpty(requestData.TargetPath))
            {
                downloadHandler = new DownloadHandlerFileCustom(requestData.TargetPath);
                requestData.Request.downloadHandler = downloadHandler;
            }
            
            lock (this)
            {
                this.currentRequest = requestData.Request;
                if(IsDebug)
                    Debug.Log("Sending request " + requestData.Request.method + " Url = " + requestData.Request.url + " timeout : " + requestData.Request.timeout.ToString());
            }

            if (requestHeaders != null)
            {
                foreach (var pair in requestHeaders)
                {
                    requestData.Request.SetRequestHeader(pair.Key, pair.Value);
                }
            }

            bool aborted = false;
            lock(this)
            {
                aborted = this.isAborted;
            }

            if (!aborted)
            {
                yield return requestData.Request.SendWebRequest();
                lock (this)
                {
                    aborted = this.isAborted;
                }
                
                if (!aborted)
                {
                    ProcessResponse(requestData.Request, responseAction);
                }
            }
        }
        finally
        {
            if (downloadHandler != null)
            {
                downloadHandler.Dispose();
            }

            requestData.Request.Dispose();
        }
    }

    public void Abort()
    {
        lock (this)
        {
            this.isAborted = true;
            this.isCompleted = true;
            this.error = null;
            this.responseData = null;
            this.isSuccess = false;
            this.message = null;
            if (this.currentRequest != null)
            {
                this.currentRequest.Abort();
                this.currentRequest = null;
            }

            if (downloadHandler != null)
            {
                downloadHandler.Dispose();
                downloadHandler = null;
            }
        }
    }
}
