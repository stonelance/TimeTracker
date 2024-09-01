using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TimeTracker
{
    public class ActivityManager
    {
        private static readonly Lazy<ActivityManager> lazy =
        new Lazy<ActivityManager>(() => new ActivityManager());

        public static ActivityManager Instance { get { return lazy.Value; } }

        private List<ActivityId> activityIds;
        public IEnumerable<ActivityId> ActivityIds
        {
            get
            {
                return activityIds;
            }
        }

        private List<ActivityId> activityIdsReverse;
        public IEnumerable<ActivityId> ActivityIdsReverse
        {
            get
            {
                return activityIdsReverse;
            }
        }

        private Dictionary<ActivityId, Configuration.ActivityConfig> activityIdToActivityConfig = new Dictionary<ActivityId,Configuration.ActivityConfig>();
        private Dictionary<string, ActivityId> activityNameToActivityId;

        public void Initialize(Configuration configuration)
        {
            if (configuration.Activities.Count(x => x.Name == "Idle") != 1)
            {
                throw new Exception("Configuration needs to include 'Idle' as an activity");
            }

            // Add all the activity types

            int nextActivityId = ActivityId.Unknown.Value + 1;
            foreach (var activity in configuration.Activities)
            {
                ActivityId activityId;
                if (activity.Name == "Unknown")
                {
                    activityId = ActivityId.Unknown;
                }
                else if (activity.Name == "NoData")
                {
                    activityId = ActivityId.NoData;
                }
                else if (activity.Name == "Away")
                {
                    activityId = ActivityId.Away;
                }
                else
                {
                    activityId = new ActivityId() { Value = nextActivityId++ };
                }

                activityIdToActivityConfig.Add(activityId, activity);
            }

            if (!activityIdToActivityConfig.ContainsKey(ActivityId.NoData))
            {
                activityIdToActivityConfig.Add(ActivityId.NoData, new Configuration.ActivityConfig() { Name = "NoData", Color = Color.LightGray, IsIncludedInRelativeTime = false });
            }

            if (!activityIdToActivityConfig.ContainsKey(ActivityId.Unknown))
            {
                activityIdToActivityConfig.Add(ActivityId.Unknown, new Configuration.ActivityConfig() { Name = "Unknown", Color = Color.Red });
            }

            if (!activityIdToActivityConfig.ContainsKey(ActivityId.Away))
            {
                activityIdToActivityConfig.Add(ActivityId.Away, new Configuration.ActivityConfig() { Name = "Away", Color = Color.LightSkyBlue, IsIncludedInRelativeTime = false });
            }

            // Generate other mappings
            activityNameToActivityId = activityIdToActivityConfig.ToDictionary(x => x.Value.Name, x => x.Key, StringComparer.OrdinalIgnoreCase);
            activityIds = activityIdToActivityConfig.Select(x => x.Key).OrderBy(x => x.Value).ToList();
            activityIdsReverse = activityIds.OrderByDescending(x => x.Value).ToList();
        }

        public ActivityId GetActivityFromName(string activityName)
        {
            return activityNameToActivityId[activityName];
        }

        public string GetNameFromActivity(ActivityId activityId)
        {
            return activityIdToActivityConfig[activityId].Name;
        }

        public Color GetColorFromActivity(ActivityId activityId)
        {
            return activityIdToActivityConfig[activityId].Color;
        }

        public bool IsIncludedInRelativeTime(ActivityId activityId)
        {
            return activityIdToActivityConfig[activityId].IsIncludedInRelativeTime;
        }

        public JObject GetPluginSettingsFromActivity(string pluginName, ActivityId activityId)
        {
            var settings = activityIdToActivityConfig[activityId].PluginSettings?.FirstOrDefault(x => String.Equals(x.PluginName, pluginName, StringComparison.OrdinalIgnoreCase))?.Settings;
            return settings != null ? JObject.FromObject(settings) : null;
        }
    }
}
