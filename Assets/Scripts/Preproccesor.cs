#if UNITY_EDITOR

using System.IO;
using Download.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class Preproccesor : IPreprocessBuildWithReport
{
    public int callbackOrder { get; }
    public void OnPreprocessBuild(BuildReport report)
    {
        //Delete download folder for smaller APK size
        var path = Constants.DownloadPath;
        if (Directory.Exists(path))
        {
            Directory.Delete(path,true);
            AssetDatabase.Refresh();
        }
        
        //Set up RuntimeControll script
        if(RuntimeControl.Instance)
            RuntimeControl.Instance.SetVrControl();
    }
}

#endif

