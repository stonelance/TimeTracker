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
        public class ActivityConfig
        {
            public string Name;
            public Color Color;
        }

        public class ProcessFocusWatcherConfig
        {
            public ProcessFocusWatcherConfig(string displayName, string activityName, string processName)
            {
                this.DisplayName = displayName;
                this.ActivityName = activityName;
                this.ProcessName = processName;
            }

            public string DisplayName;
            public string ActivityName;
            public string ProcessName;
        }

        public class ProcessActivityWatcherConfig
        {
            public ProcessActivityWatcherConfig(string displayName, string activityName, string processName, double cpuUsageThresholdForRunning, double delayBeforeReturnToInactiveInSeconds, double updatePeriodInSeconds)
            {
                this.DisplayName = displayName;
                this.ActivityName = activityName;
                this.ProcessName = processName;
                this.CPUUsageThresholdForRunning = cpuUsageThresholdForRunning;
                this.DelayBeforeReturnToInactiveInSeconds = delayBeforeReturnToInactiveInSeconds;
                this.UpdatePeriodInSeconds = updatePeriodInSeconds;
            }

            public string DisplayName;
            public string ActivityName;
            public string ProcessName;
            public double CPUUsageThresholdForRunning;
            public double DelayBeforeReturnToInactiveInSeconds;
            public double UpdatePeriodInSeconds;
        }

        public List<ActivityConfig> Activities = new List<ActivityConfig>();
        public List<ProcessFocusWatcherConfig> ProcessFocusWatchers = new List<ProcessFocusWatcherConfig>();
        public List<ProcessActivityWatcherConfig> ProcessActivityWatchers = new List<ProcessActivityWatcherConfig>();
    }
}
