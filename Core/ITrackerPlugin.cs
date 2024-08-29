using Newtonsoft.Json.Linq;

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
