using _Core.Scripts.Downolad;
using Download.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconDownloader
{
    private Action<Sprite> spriteDownloaded;
    private Action<IconDownloader> completed;
    private bool isCancelled;
    private IConnection connection;

    public IconDownloader(string url, string description, Action<Sprite> spriteDownloaded, Action<IconDownloader> completed)
    {
        if (spriteDownloaded == null)
        {
            throw new ArgumentNullException(nameof(spriteDownloaded));
        }

        if (completed == null)
        {
            throw new ArgumentNullException(nameof(completed));
        }

        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentNullException(nameof(url));
        }

        this.spriteDownloaded = spriteDownloaded;
        this.completed = completed;
        DownloadQueue queue = new DownloadQueue();
        queue.OnSuccess += Success;
        queue.OnFail += Failure;
        queue.downloadRequest = new DownloadRequest(url, description);
        connection = DownloadManager.Instance.AddDownloadQueue(queue);
    }

    public void Unsubscribe(Action<Sprite> onDownloaded)
    {
        if (onDownloaded != null && this.spriteDownloaded == onDownloaded)
        {
            this.spriteDownloaded = null;
        }
    }

    public bool IsCancelled
    {
        get => isCancelled;
        set
        {
            isCancelled = value;
            if (value)
            {
                if (this.connection != null)
                {
                    this.connection.Abort();
                    this.connection = null;
                }
            }
        }
    }

    public void Success(DownloadInfo downloadInfo)
    {
        try
        {
            if (!this.isCancelled)
            {
                Sprite sprite = null;
                try
                {
                    Texture2D t = new Texture2D(100, 100); // size is not important, this is rewritten by texture loading.
                    t.LoadImage(downloadInfo.ResponseData.byteData);
                    sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), Vector2.zero);
                }
                catch (Exception exc)
                {
                    Debug.LogError("Cannot create sprite: " + exc);
                }

                this.spriteDownloaded?.Invoke(sprite);
            }
        }
        finally
        {
            this.connection = null;
            this.completed(this);
        }
    }

    public void Failure(string error)
    {
        Debug.LogError(error);
        this.connection = null;
        this.completed(this);
    }
}
