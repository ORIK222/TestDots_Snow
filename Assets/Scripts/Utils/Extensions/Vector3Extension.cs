using UnityEngine;

namespace Download.Core.Utils.Extensions
{
    public static class Vector3Extension
    {
        public static bool IsNull(this Vector3 vector) => vector == null;
        public static bool IsZero(this Vector3 vector) => vector == Vector3.zero;

        public static bool IsNullOrZero(this Vector3 vector)
        {
            if (IsNull(vector)) return true;
            return IsZero(vector);
        }
    }
}