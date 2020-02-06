
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace TimeTracker
{
    public struct ActivityId : IEquatable<ActivityId>
    {
        public static readonly ActivityId NoData = new ActivityId() { Value = 0 };
        public static readonly ActivityId Unknown = new ActivityId() { Value = 1 };
        public static readonly ActivityId Away = new ActivityId() { Value = 65535 };

        public int Value;

        public bool Equals(ActivityId o)
        {
            return this.Value == o.Value;
        }

        public override bool Equals(object o)
        {
            return o is ActivityId && this.Value == ((ActivityId)o).Value;
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public static bool operator ==(ActivityId a, ActivityId b)
        {
            return a.Value == b.Value;
        }

        public static bool operator !=(ActivityId a, ActivityId b)
        {
            return a.Value != b.Value;
        }
    }

    public static class ActivityManager
    {
        static private List<ActivityId> activityIds;
        static public IEnumerable<ActivityId> ActivityIds
        {
            get
            {
                return activityIds;
            }
        }

        static private List<ActivityId> activityIdsReverse;
        static public IEnumerable<ActivityId> ActivityIdsReverse
        {
            get
            {
                return activityIdsReverse;
            }
        }

        static private Dictionary<ActivityId, Configuration.ActivityConfig> activityIdToActivityConfig = new Dictionary<ActivityId,Configuration.ActivityConfig>();
        static private Dictionary<string, ActivityId> activityNameToActivityId;

        public static void Initialize(Configuration configuration)
        {
            if (configuration.Activities.Count(x => x.Name == "Idle") != 1)
            {
                throw new Exception("Configuration needs to include 'Idle' as an activity");
            }

            // Add all the activity types
            activityIdToActivityConfig.Add(ActivityId.NoData, new Configuration.ActivityConfig() { Name = "NoData", Color = Colors.LightGray });
            activityIdToActivityConfig.Add(ActivityId.Unknown, new Configuration.ActivityConfig() { Name = "Unknown", Color = Colors.Red });

            int nextActivityId = ActivityId.Unknown.Value + 1;
            foreach (var activity in configuration.Activities)
            {
                activityIdToActivityConfig.Add(new ActivityId() { Value = nextActivityId++ }, activity);
            }

            activityIdToActivityConfig.Add(ActivityId.Away, new Configuration.ActivityConfig() { Name = "Away", Color = Colors.LightSkyBlue });

            // Generate other mappings
            activityNameToActivityId = activityIdToActivityConfig.ToDictionary(x => x.Value.Name, x => x.Key, StringComparer.OrdinalIgnoreCase);
            activityIds = activityIdToActivityConfig.Select(x => x.Key).OrderBy(x => x.Value).ToList();
            activityIdsReverse = activityIds.OrderByDescending(x => x.Value).ToList();
        }

        public static ActivityId GetActivityFromName(string activityName)
        {
            return activityNameToActivityId[activityName];
        }

        public static string GetNameFromActivity(ActivityId activityId)
        {
            return activityIdToActivityConfig[activityId].Name;
        }

        public static Color GetColorFromActivity(ActivityId activityId)
        {
            return activityIdToActivityConfig[activityId].Color;
        }
    }
}
