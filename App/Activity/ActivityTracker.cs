using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using TimeTracker.Properties;
using TimeTracker.ViewModel;
using TimeTracker.Watchers;
using static TimeTracker.Configuration;

namespace TimeTracker
{
    public class ActivityTracker
    {
        private DailyActivity dailyActivity;
        private WinEventDelegate winEventDelegate;
        private IntPtr winEventHook;

        public Dictionary<BaseWatcher, WatcherVM> Watchers { get; private set; }
        public List<ITrackerPlugin> Plugins { get; private set; }


        public ActivityTracker(Configuration configuration, DailyActivity dailyActivity)
        {
            this.dailyActivity = dailyActivity;

            // This doesn't seem to handle alt-tab well (or at all).  Switching to a polling method instead
            //this.winEventDelegate = new WinEventDelegate(WinEventProc);
            //this.winEventHook = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, this.winEventDelegate, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);

            this.Watchers = new Dictionary<BaseWatcher, WatcherVM>();

            Dictionary<string, Type> watcherTypes = new Dictionary<string, Type>();
            watcherTypes.Add("LockScreenWatcher", typeof(LockScreenWatcher));
            watcherTypes.Add("InputWatcher", typeof(InputWatcher));
            watcherTypes.Add("ProcessFocusWatcher", typeof(ProcessFocusWatcher));
            watcherTypes.Add("ProcessActivityWatcher", typeof(ProcessActivityWatcher));

            this.Plugins = new List<ITrackerPlugin>();
            foreach (var pluginConfig in configuration.Plugins)
            {
                Assembly pluginAssembly = LoadAssembly(pluginConfig.Path);
                if (pluginAssembly == null)
                {
                    throw new Exception($"Plugin '{pluginConfig.Path}' was not found, or could not be loaded.");
                }

                var trackerPluginType = GetTrackerPluginTypeFromAssembly(pluginAssembly);
                var watcherPluginType = GetWatcherTypeFromAssembly(pluginAssembly);

                if (trackerPluginType == null && watcherPluginType == null)
                {
                    string availableTypes = string.Join(",", pluginAssembly.GetTypes().Select(t => t.FullName));
                    throw new ApplicationException(
                        $"Can't find any type which implements ITrackerPlugin or BaseWatcher in {pluginAssembly} from {pluginAssembly.Location}.\n" +
                        $"Available types: {availableTypes}");
                }
                else if (trackerPluginType != null && watcherPluginType != null)
                {
                    throw new ApplicationException(
                        $"Assembly {pluginAssembly} from {pluginAssembly.Location} contains both an IWatcherPlugin '{trackerPluginType.FullName}' and BaseWatcher '{watcherPluginType.FullName}' when only one or the other was expected.\n");
                }
                else if (trackerPluginType != null)
                {
                    ITrackerPlugin plugin = Activator.CreateInstance(trackerPluginType) as ITrackerPlugin;
                    plugin.Initialize(configuration, ActivityManager.Instance, pluginConfig.Settings != null ? JObject.FromObject(pluginConfig.Settings) : null);
                    this.Plugins.Add(plugin);
                }
                else
                {
                    watcherTypes.Add(watcherPluginType.Name, watcherPluginType);
                }
            }

            foreach (var watcherConfig in configuration.Watchers)
            {
                var type = watcherTypes[watcherConfig.Type];
                var watcher = Activator.CreateInstance(type, watcherConfig.DisplayName, ActivityManager.Instance.GetActivityFromName(watcherConfig.ActivityName), watcherConfig.Settings != null ? JObject.FromObject(watcherConfig.Settings) : null) as BaseWatcher;
                watcher.PropertyChanged += (object obj, PropertyChangedEventArgs args) => { this.Watchers[(BaseWatcher)obj].Active = ((BaseWatcher)obj).IsActive; };
                this.Watchers.Add(watcher, new WatcherVM(watcher));
            }

            foreach (var watcher in this.Watchers)
            {
                watcher.Value.PropertyChanged += watcher_PropertyChanged;
            }

            // Force update the current activity
            watcher_PropertyChanged(null, null);
        }

        private Assembly LoadAssembly(string pluginPath)
        {
            // First try to load relative to the exe, otherwise treat as an absolute path
            var path = Path.Combine(Environment.CurrentDirectory, pluginPath);
            if (!File.Exists(path))
            {
                path = pluginPath;
            }
            if (!File.Exists(path))
            {
                return null;
            }

            return Assembly.LoadFile(path);
        }

        private Type GetTrackerPluginTypeFromAssembly(Assembly assembly)
        {
            int count = assembly.GetTypes().Count(x => typeof(ITrackerPlugin).IsAssignableFrom(x));
            if (count > 1)
            {
                throw new ApplicationException($"{assembly} from '{assembly.Location}' contains more than one implementation of ITrackerPlugin");
            }

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(ITrackerPlugin).IsAssignableFrom(type))
                {
                    return type;
                }
            }

            return null;
        }

        private Type GetWatcherTypeFromAssembly(Assembly assembly)
        {
            int count = assembly.GetTypes().Count(x => typeof(BaseWatcher).IsAssignableFrom(x));
            if (count > 1)
            {
                throw new ApplicationException($"{assembly} from '{assembly.Location}' contains more than one implementation of BaseWatcher");
            }

            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(BaseWatcher).IsAssignableFrom(type))
                {
                    return type;
                }
            }

            return null;
        }

        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            string processName = null;

            try
            {
                if (hwnd != IntPtr.Zero)
                {
                    uint processId;
                    NativeMethods.GetWindowThreadProcessId(hwnd, out processId);

                    var foregroundProcess = Process.GetProcessById((int)processId);

                    processName = foregroundProcess?.ProcessName;
                }
            }
            catch (ArgumentException)
            {
                // GetProcessById can throw this exception if it died between getting the process if and calling that function
                // If that happens, just ignore it and treat the process as not active
            }
            catch (InvalidOperationException)
            {
                // Process.ProcessName can throw this exception if it died between getting the process and calling that property
                // If that happens, just ignore it and treat the process as not active
            }

            foreach (var watcher in this.Watchers)
            {
                watcher.Key.OnForegroundProcessNameChanged(processName);
            }
        }

        public void SetDailyActivity(DailyActivity newDailyActivity, DateTime now)
        {
            lock (this)
            {
                DailyActivity oldActivity = this.dailyActivity;

                if (oldActivity.CurrentActivityRegion != null)
                {
                    Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            oldActivity.Update(now);
                        });

                    newDailyActivity.SetCurrentActivity(this.dailyActivity.CurrentActivityRegion.ActivityId, now);
                }

                this.dailyActivity = newDailyActivity;
            }
        }

        public void Update(DateTime now)
        {
            lock (this)
            {
                try
                {
                    nint hwnd = NativeMethods.GetForegroundWindow();
                    uint processId;
                    NativeMethods.GetWindowThreadProcessId(hwnd, out processId);

                    var foregroundProcess = Process.GetProcessById((int)processId);
                    if (foregroundProcess != null)
                    {
                        foreach (var watcher in this.Watchers)
                        {
                            watcher.Key.OnForegroundProcessNameChanged(foregroundProcess.ProcessName);
                        }
                    }
                }
                catch (Exception)
                {
                }

                Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        this.dailyActivity.Update(now);
                    });
            }
        }

        public void Cancel()
        {
            foreach (var watcher in this.Watchers)
            {
                watcher.Key.Cancel();
            }

            foreach (var plugin in this.Plugins)
            {
                plugin.Shutdown();
            }
        }

        private void watcher_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var now = DateTime.Now;

            HashSet<ActivityId> activeActivities = new HashSet<ActivityId>();

            foreach (var watcher in this.Watchers)
            {
                if (watcher.Value.Active)
                {
                    activeActivities.Add(watcher.Value.Activity);
                }
            }

            ActivityId activeActivity = ActivityId.Unknown;
            foreach (var activityId in ActivityManager.Instance.ActivityIdsReverse)
            {
                if (activeActivities.Contains(activityId))
                {
                    activeActivity = activityId;
                    break;
                }
            }

            lock (this)
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        bool activityChanged = this.dailyActivity.CurrentActivityRegion?.ActivityId != activeActivity;

                        this.dailyActivity.SetCurrentActivity(activeActivity, now);

                        if (activityChanged)
                        {
                            foreach (var plugin in this.Plugins)
                            {
                                plugin.OnActiveActivityChanged(this.dailyActivity.CurrentActivityRegion.ActivityId);
                            }
                        }
                    });
            }
        }
    }
}
