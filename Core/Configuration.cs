using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Drawing;

namespace TimeTracker
{
    public class Configuration
    {
        public class PluginConfig
        {
            public string Path;

            [JsonExtensionData]
            public Dictionary<string, JToken> Settings;
        }

        public class ActivityConfig
        {
            public class PluginConfig
            {
                public string PluginName;

                [JsonExtensionData]
                public Dictionary<string, JToken> Settings;
            }

            public string Name;
            public Color Color;
            public List<PluginConfig> PluginSettings;
        }

        public class BaseWatcherConfig
        {
            public string Type;
            public string DisplayName;
            public string ActivityName;

            [JsonExtensionData]
            public Dictionary<string, JToken> Settings;
        }

        public List<PluginConfig> Plugins = new List<PluginConfig>();
        public List<ActivityConfig> Activities = new List<ActivityConfig>();
        public List<BaseWatcherConfig> Watchers = new List<BaseWatcherConfig>();
    }
}
