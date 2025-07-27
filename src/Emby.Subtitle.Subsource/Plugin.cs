using System;
using System.IO;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;

namespace Emby.Subtitle.SubSource
{
    public class Plugin : BasePlugin, IHasThumbImage
    {
        public override Guid Id => new Guid("01984ab6-e3d2-7d4a-be16-6a6553c4de5c");

        public override string Name => Const.PluginName;

        public override string Description => "Download subtitles from SubSource";

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }
    }
}
