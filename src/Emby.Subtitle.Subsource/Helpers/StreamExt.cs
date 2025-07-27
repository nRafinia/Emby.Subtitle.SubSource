using System.IO;

namespace Emby.Subtitle.SubSource.Helpers
{
    internal static class StreamExt
    {
        public static string ReadToEnd(this Stream stream)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}