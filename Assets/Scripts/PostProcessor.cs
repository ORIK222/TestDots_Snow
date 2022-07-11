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
            if(RuntimeControl.Instance)
                RuntimeControl.Instance.SetLastControl();   
        }
    }
}
#endif
