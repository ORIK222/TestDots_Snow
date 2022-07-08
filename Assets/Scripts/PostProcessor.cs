#if UNITY_EDITOR


using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Download.Core
{
    public class PostProcessor : IPostprocessBuildWithReport
    {
        public int callbackOrder { get; }
        
        public void OnPostprocessBuild(BuildReport report)
        {
            RuntimeControl.Instance.SetLastControl();   
        }
    }
}
#endif
