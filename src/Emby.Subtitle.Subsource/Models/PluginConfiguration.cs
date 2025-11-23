using System.ComponentModel;
using Emby.Web.GenericEdit;

namespace Emby.Subtitle.SubSource.Models
{
    public class PluginConfiguration : EditableOptionsBase
    {
        public override string EditorTitle => "SubSource options";
        
        [DisplayName("API Key")]
        [Description("<span style='color:red'>Hint: please restart Emby after set API Key</span><br/>This website uses an API key to control and limit the number of search and subtitle download requests. This plugin is only a connector to that service. Please visit <a href=\"https://subsource.net/\" target=\"_blank\" rel=\"noopener\">https://subsource.net/</a> and create a user account. Then, at the bottom of the page\n                    <a href=\"https://subsource.net/dashboard/profile\" target=\"_blank\" rel=\"noopener\">https://subsource.net/dashboard/profile</a> you can generate an API key for yourself. After creating the API key, enter it below.")]
        public string ApiKey { get; set; } = string.Empty;
    }
}
