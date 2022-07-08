using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR 
using UnityEditor;
#endif


/// <summary>
/// Stores data in persistent data storage.
/// </summary>
public static class PersistentCache
{
    public static bool Initialized = false;
    private static string persistentDataPath;
    public static string cacheDataPath;

    public static TimeSpan MaxCacheKeepingTime;

    public static void Init()
    {
        Initialized = true;
        MaxCacheKeepingTime = new TimeSpan(9999, 0, 0, 0);
        persistentDataPath = Application.persistentDataPath;
        cacheDataPath = Path.Combine(persistentDataPath, "PersistentCache");
        
        
        if (!Directory.Exists(cacheDataPath))
            Directory.CreateDirectory(cacheDataPath);
        
    }

    /// <summary>
    /// Clean cache directory
    /// </summary>
    public static void ClearPersistentStorage()
    {
        if (!Initialized)
            Init();

        if (Directory.Exists(cacheDataPath))
        {
            Directory.Delete(cacheDataPath, true);
            Directory.CreateDirectory(cacheDataPath);
        }
    }

    public static void ClearResourcesStorage()
    {
        if(!Initialized)
            Init();
        
        
    }

    public static T TryLoad<T>(SaveDirectory directory,bool removeIfOutOfDate = false)
    {
        return TryLoad<T>(typeof(T).Name,directory,removeIfOutOfDate);
    }
    public static T TryLoad<T>(bool removeIfOutOfDate = false)
    {
        return TryLoad<T>(typeof(T).Name,SaveDirectory.PersistentCache,removeIfOutOfDate);
    }

    public static T TryLoad<T>(string key, bool removeIfOutOfDate = false)
    {
        return TryLoad<T>(key, SaveDirectory.PersistentCache);
    }
    public static T TryLoad<T>(string key,SaveDirectory directory, bool removeIfOutOfDate = false)
    {
        if (!Initialized)
            Init();

        var fullPath = GetPath(directory,key);
        if (File.Exists(fullPath))
                try
                {
                    var lastWrite = File.GetLastWriteTime(fullPath);
                    if (!removeIfOutOfDate || DateTime.Now - lastWrite < MaxCacheKeepingTime)
                    {
                        using (var fs = File.OpenRead(fullPath))
                        {
                            return (T) new BinaryFormatter().Deserialize(fs);
                        }
                    }
                    else
                    {
                        File.Delete(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

            return default(T);
    }

    public static bool Save<T>(string key, T data)
    {
        return Save(key, SaveDirectory.PersistentCache, data);
    }
    public static bool Save<T>(T data, SaveDirectory directory)
    {
        return Save(data.GetType().Name,directory, data);
    }
    public static bool Save<T>(T data)
    {
        return Save(data.GetType().Name,SaveDirectory.PersistentCache, data);
    }

    public static bool Save<T>(string key,SaveDirectory directory, T data)
    {
        if (!Initialized)
            Init();

        var fullPath = GetPath(directory,key);
        try
        {
            using (var fs = File.Create(fullPath))
                new BinaryFormatter().Serialize(fs, data);

            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        return false;
    }

#if UNITY_EDITOR
    
    [MenuItem("Tools/PersistentCache/Refresh asset database")]
    public static void RefreshAssets()
    {
        AssetDatabase.Refresh();
    }
#endif

    public static byte[] TryLoad(string key,SaveDirectory directory, bool removeIfOutOfDate = false)
    {
        if (!Initialized)
            Init();

        var fullPath = GetPath(directory,key);
        if (File.Exists(fullPath))
        try
        {
            var lastWrite = File.GetLastWriteTime(fullPath);

            if (!removeIfOutOfDate || DateTime.Now - lastWrite < MaxCacheKeepingTime)
            {
                return File.ReadAllBytes(fullPath);
            }
            else
            {
                File.Delete(fullPath);
            }
        }
        catch { }

        return null;
    }

    static string Save(string key,SaveDirectory directory, byte[] bytes)
    {
        if (!Initialized)
            Init();

        var fullPath = GetPath(directory,key);
        File.WriteAllBytes(fullPath, bytes);
        return fullPath;
    }

    public static string GetPath(SaveDirectory directory,string key)
    {
        if(directory == SaveDirectory.PersistentCache)
            return Path.Combine(cacheDataPath, UnityWebRequest.EscapeURL(key));
        else if (directory == SaveDirectory.Resources)
            return Path.Combine(persistentDataPath, key);
        else return null;
    }

    public static bool Remove(string key,SaveDirectory directory)
    {
        var fullName = GetPath(directory,key);
        if (File.Exists(fullName))
        {
            File.Delete(fullName);
            return true;
        }

        return false;
    }
    public enum SaveDirectory
    {
        Resources,
        PersistentCache
    }

}