namespace Download.Core.Utils.Extensions
{
    public static class StringExtension
    {
        public static bool Contains(this string original, string[] names)
        {
            foreach (var name in names)
                if (original.Contains(name)) return true;

            return false;
        }

        public static bool IsNullOrEmpty(this string origin) => string.IsNullOrEmpty(origin);
    }
}