namespace Art_BaBomb.Web.Helpers
{
    public static class FileSizeHelper
    {
        public static string FormatFileSize(long? bytes)
        {
            if (!bytes.HasValue)
            {
                return string.Empty;
            }

            if (bytes.Value < 1024)
            {
                return $"{bytes.Value} B";
            }

            if (bytes.Value < 1024 * 1024)
            {
                return $"{bytes.Value / 1024.0:F1} KB";
            }

            return $"{bytes.Value / (1024.0 * 1024.0):F1} MB";
        }
    }
}