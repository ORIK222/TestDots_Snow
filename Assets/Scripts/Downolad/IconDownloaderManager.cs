using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class IconDownloaderManager
{
    private static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    private static List<IconDownloader> iconDownloaders = new List<IconDownloader>();

    public static Sprite GetSprite(string url)
    {
        Sprite sprite = null;
        if (sprites.ContainsKey(url))
        {
            sprite = sprites[url];
        }

        return sprite;
    }

    public static void DownloadSprite(string url, string description, Action<Sprite> onDownloaded)
    {
        if (onDownloaded == null)
        {
            throw new ArgumentNullException(nameof(onDownloaded));
        }

        var sprite = GetSprite(url);
        if (sprite == null)
        {
            iconDownloaders.Add(new IconDownloader(url, description,
                (Sprite spriteDownloaded) =>
                {
                    if (spriteDownloaded != null)
                    {
                        sprites[url] = spriteDownloaded;
                        onDownloaded?.Invoke(spriteDownloaded);
                    }
                }
                , SpriteDownloadingFinished));
        }
        else
        {
            onDownloaded.Invoke(sprite);
        }
    }

    public static void Unsubscribe(Action<Sprite> onDownloaded)
    {
        foreach(var downloader in iconDownloaders)
        {
            downloader.Unsubscribe(onDownloaded);
        }
    }

    private static void SpriteDownloadingFinished(IconDownloader downloader)
    {
        iconDownloaders.Remove(downloader);
    }
}
