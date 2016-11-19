
namespace RemoteTech.Common.Extensions
{
    public static class StringExtension
    {
        public static string Truncate(this string targ, int len)
        {
            const string suffix = "...";
            if (targ.Length > len)
            {
                return targ.Substring(0, len - suffix.Length) + suffix;
            }
            return targ;
        }
    }
}
