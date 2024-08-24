using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

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
            public string DisplayName;
            public string ActivityName;
        }

        public class ProcessWatcherConfig : BaseWatcherConfig
        {
            public string ProcessName;
        }

        public class ProcessFocusWatcherConfig : ProcessWatcherConfig
        {
        }

        public class ProcessActivityWatcherConfig : ProcessWatcherConfig
        {
            public double CPUUsageThresholdForRunning;
            public double DelayBeforeReturnToInactiveInSeconds;
            public double UpdatePeriodInSeconds;
        }

        public class PluginWatcherConfig : BaseWatcherConfig
        {
            public string Path;

            [JsonExtensionData]
            public Dictionary<string, JToken> Settings;
        }

        public List<PluginConfig> Plugins = new List<PluginConfig>();
        public List<ActivityConfig> Activities = new List<ActivityConfig>();
        public List<ProcessFocusWatcherConfig> ProcessFocusWatchers = new List<ProcessFocusWatcherConfig>();
        public List<ProcessActivityWatcherConfig> ProcessActivityWatchers = new List<ProcessActivityWatcherConfig>();
        public List<PluginWatcherConfig> PluginWatchers = new List<PluginWatcherConfig>();
    }
}
