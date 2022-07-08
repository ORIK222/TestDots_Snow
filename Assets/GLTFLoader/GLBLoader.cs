using System;
using System.Collections;
using System.IO;
using DefaultNamespace.GLTFLoader;
using TiltBrushToolkit;
using UnityEngine;

    //Master
public static class GLBLoader
{
   static GltfImportOptions kOptions = new GltfImportOptions {
        rescalingMode = GltfImportOptions.RescalingMode.CONVERT,
        scaleFactor = 1,
        recenter = false,
    };
    
    public static AsyncController Load(string path, Action<GameObject> onLoaded, Action<Exception> onFailed, string name = "GLTFimport")
    {
        AsyncController controller = null;
        var streamLoader = File.OpenRead(path);
        try
        {
            Debug.Log("Loading model from path " + path);
            if (GltfImportManager.GetGenerator(path).Contains("Tilt Brush"))
            {
                IUriLoader uriLoader = new BufferedStreamLoader(streamLoader, Path.GetDirectoryName(path));
                controller = ImportGltf.Import(path, name, uriLoader, null,
                    (AsyncController controller) => onLoaded?.Invoke(controller.Result.root), onFailed, kOptions);
            }
            else
            {
                Siccity.GLTFUtility.ImportSettings settings = new Siccity.GLTFUtility.ImportSettings();
                settings.materials = true;
                settings.generateLightmapUVs = false;
                settings.hardAngle = 30;
                settings.areaError = 10;
                settings.angleError = 10;
                GameObject obj = new GameObject();
                controller = obj.AddComponent<AsyncController>();
                controller.Failure = onFailed;
                controller.Result = new ImportGltf.GltfImportResult();
                Action<GameObject, AnimationClip[]> final = (GameObject imported, AnimationClip[] clips) =>
                {
                    if (controller != null && controller.gameObject != null)
                    {
                        try
                        {
                            controller.Result.root = imported;
                            controller.Finish();
                            onLoaded?.Invoke(imported);
                        }
                        catch (Exception exc)
                        {
                            GameObject.Destroy(imported);
                            Debug.LogError(exc);
                        }
                    }
                    else
                    {
                        GameObject.Destroy(imported);
                    }
                };
                Siccity.GLTFUtility.Importer.ImportGLBAsync(path, settings, final);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
            streamLoader.Dispose();
            throw;
        }

        return controller;
    }

    public static void LoadTiltBrush(Stream stream, Action<GameObject> onLoaded, Action<Exception> onFailed, string name = "GLTFimport")
    {
        BufferedStreamLoader uriLoader = new BufferedStreamLoader(stream, null);
        try
        {
            ImportGltf.Import(stream, name, uriLoader, null,
                (AsyncController controller) => onLoaded?.Invoke(controller.Result.root), onFailed, kOptions);
        }
        catch
        {
            uriLoader.Dispose();
            throw;
        }
    }
}
