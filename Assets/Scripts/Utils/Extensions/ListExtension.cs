using System.Collections;

namespace Download.Core.Utils.Extensions
{
    public static class ListExtension
    {

        public static bool IsNull(this IList list) => list == null;
        public static bool IsEmpty(this IList list) => list.Count > 0;

        public static bool IsNullOrEmpty(this IList list)
        {
            return IsNull(list) || IsEmpty(list);
        }
    }
}