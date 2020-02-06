using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using TimeTracker.ViewModel;
using TimeTracker.Watchers;

namespace TimeTracker
{
    public class ActivityTracker
    {
        private DailyActivity dailyActivity;
		private WinEventDelegate winEventDelegate;
		private IntPtr winEventHook;

		public Dictionary<BaseWatcher, WatcherVM> Watchers { get; private set; }


        public ActivityTracker(Configuration configuration, DailyActivity dailyActivity)
        {
            this.dailyActivity = dailyActivity;

			this.winEventDelegate = new WinEventDelegate(WinEventProc);
			this.winEventHook = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, this.winEventDelegate, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);

			this.Watchers = new Dictionary<BaseWatcher, WatcherVM>();

            var lockScreenWatcher = new LockScreenWatcher("Lock Screen", ActivityId.Away);
            lockScreenWatcher.PropertyChanged += (object obj, PropertyChangedEventArgs args) => { this.Watchers[(BaseWatcher)obj].Active = ((LockScreenWatcher)obj).CurrentState == LockScreenWatcher.State.Active; };
            this.Watchers.Add(lockScreenWatcher, new WatcherVM(lockScreenWatcher));

            var inputWatcher = new InputWatcher("Idle", ActivityManager.GetActivityFromName("Idle"), 60, 1000);
            inputWatcher.PropertyChanged += (object obj, PropertyChangedEventArgs args) => { this.Watchers[(BaseWatcher)obj].Active = ((InputWatcher)obj).CurrentState == InputWatcher.State.Inactive; };
            this.Watchers.Add(inputWatcher, new WatcherVM(inputWatcher));

            foreach (var watcherConfig in configuration.ProcessFocusWatchers)
            {
                var watcher = new ProcessFocusWatcher(watcherConfig.DisplayName, ActivityManager.GetActivityFromName(watcherConfig.ActivityName), watcherConfig.ProcessName);
                watcher.PropertyChanged += (object obj, PropertyChangedEventArgs args) => { this.Watchers[(BaseWatcher)obj].Active = ((ProcessFocusWatcher)obj).CurrentState == ProcessFocusWatcher.State.Active; };
                this.Watchers.Add(watcher, new WatcherVM(watcher));
            }

            foreach (var watcherConfig in configuration.ProcessActivityWatchers)
            {
                var watcher = new ProcessActivityWatcher(watcherConfig.DisplayName, ActivityManager.GetActivityFromName(watcherConfig.ActivityName), watcherConfig.ProcessName, watcherConfig.CPUUsageThresholdForRunning, watcherConfig.DelayBeforeReturnToInactiveInSeconds, watcherConfig.UpdatePeriodInSeconds);
                watcher.PropertyChanged += (object obj, PropertyChangedEventArgs args) => { this.Watchers[(BaseWatcher)obj].Active = ((ProcessActivityWatcher)obj).CurrentState == ProcessActivityWatcher.State.Running; };
                this.Watchers.Add(watcher, new WatcherVM(watcher));
            }

            foreach (var watcher in this.Watchers)
            {
                watcher.Value.PropertyChanged += watcher_PropertyChanged;
            }

            // Force update the current activity
            watcher_PropertyChanged(null, null);
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
				(watcher.Key as ProcessFocusWatcher)?.OnForegroundProcessNameChanged(processName);
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
            foreach (var activityId in ActivityManager.ActivityIdsReverse)
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
                        this.dailyActivity.SetCurrentActivity(activeActivity, now);
                    });
            }
        }
    }
}
