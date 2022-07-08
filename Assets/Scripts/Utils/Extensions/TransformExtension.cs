using UnityEngine;

namespace Download.Core.Utils.Extensions
{
    public static class TransformExtension
    {
        public static void Activate(this Transform tranform) => tranform.gameObject.SetActive(true);
        public static void Deactivate(this Transform tranform) => tranform.gameObject.SetActive(false);

        public static Transform[] GetChilds(this Transform transform)
        {
            var childs = transform.childCount;
            var childsArray = new Transform[childs];
            for (int i = 0; i < childs; i++)
                childsArray[i] = transform.GetChild(i);

            return childsArray;
        }
    }
}