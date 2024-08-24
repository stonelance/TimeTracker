using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTracker
{
    public interface ITrackerPlugin
    {
        string Name { get; }
        string Description { get; }

        void Initialize(Configuration configuration, ActivityManager activitymanager, JObject settings);
        void OnActiveActivityChanged(ActivityId activityId);
        void Shutdown();
    }
}
