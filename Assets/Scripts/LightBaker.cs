using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if UNITY_EDITOR

public class LightBaker : EditorWindow
{
    private GameObject source;
    private GameObject destination;
    private string bakedText;
    private bool isPrepared;
    private Vector2 scrollPos;

    [MenuItem("GameObject/Bake light into Prefab")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        LightBaker window = EditorWindow.GetWindow<LightBaker>();
        if (Selection.gameObjects.Length > 0)
        {
            window.source = Selection.gameObjects[0];
        }
        if (Selection.gameObjects.Length > 1)
        {
            window.destination = Selection.gameObjects[1];
        }

        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Baked light data copy", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(isPrepared))
        {
            source = (GameObject)EditorGUILayout.ObjectField("Source", source, typeof(GameObject), true);
            destination = (GameObject)EditorGUILayout.ObjectField("Destination", destination, typeof(GameObject), true);
        }

        GUILayout.Label("Prepared data:", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        bakedText = EditorGUILayout.TextArea(bakedText, GUIStyle.none);
        EditorGUILayout.EndScrollView();
        if (!isPrepared && source != null && destination != null)
        {
            if (GUILayout.Button("Prepare"))
            {
                isPrepared = true;
            }
        }

        if (isPrepared)
        {
            var sourceHierarchy = GetHierarchy(source);
            var destHierarchy = GetHierarchy(destination);
            if (sourceHierarchy.Count != destHierarchy.Count)
            {
                isPrepared = false;
                EditorUtility.DisplayDialog("Error", "Only duplicates of game objects having same hierarchy may be selected!" + Environment.NewLine +
                    "Source hierarchy active objects count: " + sourceHierarchy.Count + Environment.NewLine +
                    "Destination hierarchy active objects count: " + destHierarchy.Count + Environment.NewLine, "ok");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                int index = 0;
                foreach(var s in sourceHierarchy)
                {
                    var d = destHierarchy[index++];
                    sb.AppendLine(GetFullPath(s.filter.gameObject, source) + " >>> " + GetFullPath(d.filter.gameObject, destination));
                }

                bakedText = sb.ToString();
                if (GUILayout.Button("Process"))
                {
                    Process(source, destination);
                }
            }
        }
    }

    private static void Process(GameObject sourceObject, GameObject destObject)
    {
        if (sourceObject != null && destObject != null)
        {
            int count = 0;
            var source = GetHierarchy(sourceObject);
            var dest = GetHierarchy(destObject);
            Debug.Log("Source = " + sourceObject.name + " dest = " + destObject.name);
            if (source.Count != dest.Count)
            {
                Debug.LogError("Only duplicates of game objects having same hierarchy may be selected!");
            }
            else
            {
                int index = 0;
                foreach (var s in source)
                {
                    var destFilter = dest[index].filter;
                    var destRenderer = dest[index].renderer;
                    GameObject go = destFilter.gameObject;
                    Mesh meshToModify = dest[index++].filter.sharedMesh;
                    if (meshToModify == null)
                    {
                        continue;
                    }

                    List<Material> materials = new List<Material>();
                    destRenderer.GetMaterials(materials);
                    bool canModify = materials.All(x => x.name.StartsWith("LM_"));
                    if (canModify)
                    {
                        Vector4 lightmapOffsetAndScale = s.renderer.lightmapScaleOffset;
                        //this code leads to issues when copying baked light.
                        //Vector2[] modifiedUV2s = meshToModify.uv2;
                        //for (int i = 0; i < meshToModify.uv2.Length; i++)
                        //{
                        //    modifiedUV2s[i] = new Vector2(meshToModify.uv2[i].x * lightmapOffsetAndScale.x +
                        //    lightmapOffsetAndScale.z, meshToModify.uv2[i].y * lightmapOffsetAndScale.y +
                        //    lightmapOffsetAndScale.w);
                        //}
                        //meshToModify.uv2 = modifiedUV2s;
                        count++;
                        Debug.Log(s.renderer.lightmapIndex);
                        Debug.Log(s.renderer.lightmapScaleOffset);

                        foreach (var material in materials)
                        {
                            if (material.name.StartsWith("LM_"))
                            {
                                material.SetTextureScale("_Lightmap", new Vector2(s.renderer.lightmapScaleOffset.x, s.renderer.lightmapScaleOffset.y));
                                material.SetTextureOffset("_Lightmap", new Vector2(s.renderer.lightmapScaleOffset.z, s.renderer.lightmapScaleOffset.w));
                            }
                            else
                            {
                                Debug.Log("Skipped material " + material.name + " since it is a non light mapped material.");
                            }
                        }

                        Debug.Log("Modified: " + GetFullPath(go));
                    }
                }

                Debug.Log("Updated GameObject's meshes: " + count);
            }
        }
    }

    private static string GetFullPath(GameObject go, GameObject rootMax = null)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(go.name);
        while ((rootMax == null && go != null) || (rootMax != null && go != rootMax && go != null))
        {
            if (go.transform.parent != null)
            {
                go = go.transform.parent.gameObject;
                if (go != null)
                {
                    sb.Insert(0, go.name + " - ");
                }
            }
            else
            {
                go = null;
            }
        }

        return sb.ToString();
    }

    private static List<(MeshFilter filter, Renderer renderer)> GetHierarchy(GameObject go)
    {
        List<(MeshFilter filter, Renderer renderer)> result = new List<(MeshFilter filter, Renderer renderer)>();
        var filters = go.GetComponentsInChildren<MeshFilter>();
        foreach (var filter in filters)
        {
            var renderer = filter.gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                (MeshFilter filter, Renderer renderer) r = (filter, renderer);
                result.Add(r);
            }
        }

        return result;
    }
}

#endif