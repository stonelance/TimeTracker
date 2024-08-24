using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace TimeTracker
{
    [Serializable]
    public class DailyActivity : INotifyPropertyChanged
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ObservableCollection<ActivityRegion> ActivityRegions { get; set; }

        [XmlIgnore]
        public ActivityRegion CurrentActivityRegion { get; private set; }
        [XmlIgnore]
        public Dictionary<ActivityId, TimeSpan> ActivitySummaries { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private DateTime lastUpdateTime;


        public DailyActivity()
        {
            var now = DateTime.Now;

            this.StartTime = now;
            this.EndTime = now;
            this.lastUpdateTime = now;
            this.ActivityRegions = new ObservableCollection<ActivityRegion>();
            this.ActivitySummaries = ActivityManager.Instance.ActivityIds.ToDictionary(x => x, x => new TimeSpan());
        }

        public DailyActivity(DateTime now)
        {
            this.StartTime = now;
            this.EndTime = now;
            this.lastUpdateTime = now;
            this.ActivityRegions = new ObservableCollection<ActivityRegion>();
            this.ActivitySummaries = ActivityManager.Instance.ActivityIds.ToDictionary(x => x, x => new TimeSpan());
        }

        public void Serialize(string path)
        {
            lock (this)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DailyActivity));
                using (TextWriter textWriter = new StreamWriter(path))
                {
                    serializer.Serialize(textWriter, this);
                }
            }
        }

        public static DailyActivity Deserialize(string path)
        {
            using (TextReader textReader = new StreamReader(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DailyActivity));
                var dailyActivity = (DailyActivity)serializer.Deserialize(textReader);

                // Repopulate the summary
                foreach (var region in dailyActivity.ActivityRegions)
                {
                    dailyActivity.ActivitySummaries[region.ActivityId] += region.Duration;
                }

                return dailyActivity;
            }
        }

        public void SetCurrentActivity(ActivityId activity, DateTime now)
        {
            lock (this)
            {
                this.EndTime = now;

                if (this.CurrentActivityRegion == null || this.CurrentActivityRegion.ActivityId != activity)
                {
                    if (this.CurrentActivityRegion != null)
                    {
                        // Wrap up the previous activity
                        this.ActivitySummaries[this.CurrentActivityRegion.ActivityId] += now - this.lastUpdateTime;
                        this.CurrentActivityRegion.EndTime = now;
                    }
                    else if (this.ActivityRegions.Count > 0)
                    {
                        // We probably loaded from disk, fill the gap with a nodata region
                        var lastRegion = this.ActivityRegions[this.ActivityRegions.Count - 1];
                        var newRegion = new ActivityRegion(lastRegion.EndTime, now, ActivityId.NoData);
                        this.ActivitySummaries[newRegion.ActivityId] += newRegion.Duration;
                        Application.Current.Dispatcher.Invoke(() =>
                            {
                                this.ActivityRegions.Add(newRegion);
                            });
                    }

                    // Start new activity
                    this.CurrentActivityRegion = new ActivityRegion(now, now, activity);
                    Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.ActivityRegions.Add(this.CurrentActivityRegion);
                        });


                    // TODO: Perform plugin actions\fire OnActivityChanged event?
                    /*
                    {
                        var activityColor = ActivityManager.Instance.GetColorFromActivity(activity);

                        ProcessStartInfo psi = new ProcessStartInfo();
                        psi.FileName = "C:\\Users\\stone\\AppData\\Local\\Microsoft\\WindowsApps\\pythonw.exe";
                        psi.Arguments = $"\"D:\\Projects\\Twinkly\\twinkly_square.py\" {activityColor.R} {activityColor.G} {activityColor.B}";
                        psi.UseShellExecute = true;
                        using (Process process = Process.Start(psi))
                        {
                        }
                    }
                    */
                }

                this.lastUpdateTime = now;
            }

            OnPropertyChanged("EndTime");
            OnPropertyChanged("ActivitySummaries");
            OnPropertyChanged("CurrentActivityRegion");
            OnPropertyChanged("ActivityRegions");
        }

        public void Update(DateTime now)
        {
            lock (this)
            {
                if (now <= this.EndTime)
                {
                    // These calls have the potential to get out of order, so just ignore them if they are too late.
                    return;
                }

                this.EndTime = now;
                this.ActivitySummaries[this.CurrentActivityRegion.ActivityId] += now - this.lastUpdateTime;
                this.CurrentActivityRegion.EndTime = now;
                this.lastUpdateTime = now;
            }

            OnPropertyChanged("EndTime");
            OnPropertyChanged("ActivitySummaries");
        }
    }
}
