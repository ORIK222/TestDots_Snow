using UnityEngine;

namespace Download.Core.Utils.Extensions
{
    public static class GameObjectExtension
    {
        public static void Activate(this GameObject gameObject) => gameObject.SetActive(true);
        public static void Deactivate(this GameObject gameObject) => gameObject.SetActive(false);

        public static GameObject[] GetChilds(this GameObject gameObject)
        {
            var childs = gameObject.transform.childCount;
            var childsArray = new GameObject[childs];
            for (int i = 0; i < childs; i++)
                childsArray[i] = gameObject.transform.GetChild(i).gameObject;

            return childsArray;
        }
    }
}