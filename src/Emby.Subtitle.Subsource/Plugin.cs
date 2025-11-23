using System;
using System.IO;
using Emby.Subtitle.SubSource.Models;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Common;
using MediaBrowser.Controller.Plugins;

namespace Emby.Subtitle.SubSource
{
    public class Plugin : BasePluginSimpleUI<PluginConfiguration>, IHasThumbImage
    {
        public static Plugin Instance { get; private set; }
        private PluginConfiguration? _configuration;

        public Plugin(IApplicationHost applicationHost) : base(applicationHost)
        {
            Instance = this;
        }

        public override Guid Id => new Guid("01984ab6-e3d2-7d4a-be16-6a6553c4de5c");

        public override string Name => Const.PluginName;

        public override string Description => "Download subtitles from SubSource";

        public ImageFormat ThumbImageFormat => ImageFormat.Png;

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        protected override void OnOptionsSaved(PluginConfiguration options)
        {
            base.OnOptionsSaved(options);
            _configuration = options;
        }

        public virtual PluginConfiguration GetConfiguration()
        {
            return _configuration?? this.GetOptions();
        }
    }
}