using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TimeTracker
{
    public class DailyActivityAggregate : INotifyPropertyChanged
    {
        public ObservableCollection<ActivityRegion> ActivityRegions { get; private set; }
        public Dictionary<ActivityId, TimeSpan> ActivitySummaries { get; private set; }
        public ActivityRegion CurrentActivityRegion { get; private set; }

        private bool isShowingOvernightAway = false;
        public bool IsShowingOvernightAway
        {
            get
            {
                return this.isShowingOvernightAway;
            }
            set
            {
                if (this.isShowingOvernightAway == value)
                {
                    return;
                }

                this.isShowingOvernightAway = value;
                Update();
            }
        }

        private Dictionary<ActivityId, TimeSpan> nMinusOneActivitySummaries { get; set; }
        private List<DailyActivity> dailyActivities = new List<DailyActivity>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public DailyActivityAggregate()
        {
            this.ActivityRegions = new ObservableCollection<ActivityRegion>();
            this.ActivitySummaries = new Dictionary<ActivityId, TimeSpan>();
            this.CurrentActivityRegion = null;
        }

        public void SetDailyActivities(IEnumerable<DailyActivity> dailyActivities)
        {
            foreach (var dailyActivity in this.dailyActivities)
            {
                dailyActivity.ActivityRegions.CollectionChanged -= ActivityRegions_CollectionChanged;
                dailyActivity.PropertyChanged -= lastDailyActivity_PropertyChanged;
            }

            this.dailyActivities = dailyActivities.ToList();
            this.dailyActivities.Sort((x, y) => x.StartTime < y.StartTime ? -1 : 1);
            Update();
        }

        private void Update()
        {
            this.nMinusOneActivitySummaries = new Dictionary<ActivityId, TimeSpan>();
            List<ActivityRegion> aggregateRegions = new List<ActivityRegion>();

            for (int i = 0; i < this.dailyActivities.Count - 1; ++i)
            {
                var dailyActivity = this.dailyActivities[i];
                foreach (var entry in dailyActivity.ActivitySummaries)
                {
                    if (nMinusOneActivitySummaries.ContainsKey(entry.Key))
                    {
                        nMinusOneActivitySummaries[entry.Key] += entry.Value;
                    }
                    else
                    {
                        nMinusOneActivitySummaries.Add(entry.Key, entry.Value);
                    }
                }
            }

            ActivityRegion prevRegion = null;
            for (int i = 0; i < this.dailyActivities.Count; ++i)
            {
                var dailyActivity = this.dailyActivities[i];
                if (prevRegion != null)
                {
                    var nextFirstRegion = dailyActivity.ActivityRegions[0];
                    if (prevRegion.EndTime != nextFirstRegion.StartTime)
                    {
                        // If there is a gap between daily activity logs, fill it in
                        if (nextFirstRegion.StartTime < prevRegion.EndTime)
                        {
                            throw new Exception("DailyActivities have not been sorted correctly");
                        }

                        var newRegion = new ActivityRegion(prevRegion.EndTime, nextFirstRegion.StartTime, ActivityId.NoData);
                        aggregateRegions.Add(newRegion);

                        if (nMinusOneActivitySummaries.ContainsKey(ActivityId.NoData))
                        {
                            nMinusOneActivitySummaries[ActivityId.NoData] += newRegion.Duration;
                        }
                        else
                        {
                            nMinusOneActivitySummaries.Add(ActivityId.NoData, newRegion.Duration);
                        }
                    }
                }

                aggregateRegions.AddRange(dailyActivity.ActivityRegions.Where(x => this.isShowingOvernightAway || x.ActivityId != ActivityId.Away || (x.StartTime != x.StartTime.Date && x.EndTime != x.EndTime.Date)));
                if (dailyActivity.ActivityRegions.Count > 0)
                {
                    prevRegion = dailyActivity.ActivityRegions[dailyActivity.ActivityRegions.Count - 1];
                }
            }

            var lastDailyActivity = this.dailyActivities[this.dailyActivities.Count - 1];
            var aggregateSummary = new Dictionary<ActivityId, TimeSpan>(nMinusOneActivitySummaries);
            foreach (var entry in lastDailyActivity.ActivitySummaries)
            {
                if (aggregateSummary.ContainsKey(entry.Key))
                {
                    aggregateSummary[entry.Key] += entry.Value;
                }
                else
                {
                    aggregateSummary.Add(entry.Key, entry.Value);
                }
            }

            this.CurrentActivityRegion = null;
            if (lastDailyActivity.StartTime.Date == DateTime.Now.Date)
            {
                this.CurrentActivityRegion = lastDailyActivity.CurrentActivityRegion;
                lastDailyActivity.ActivityRegions.CollectionChanged += ActivityRegions_CollectionChanged;
                lastDailyActivity.PropertyChanged += lastDailyActivity_PropertyChanged;
            }

            Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    this.ActivityRegions.Clear();
                    foreach (var item in aggregateRegions)
                    {
                        this.ActivityRegions.Add(item);
                    }
                });
            this.ActivitySummaries = aggregateSummary;

            OnPropertyChanged("ActivitySummaries");
            OnPropertyChanged("CurrentActivityRegion");
        }

        private void lastDailyActivity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var dailyActivity = (DailyActivity)sender;

            if (e.PropertyName == "ActivitySummaries")
            {
                var aggregateSummary = new Dictionary<ActivityId, TimeSpan>(this.nMinusOneActivitySummaries);

                foreach (var entry in dailyActivity.ActivitySummaries)
                {
                    if (aggregateSummary.ContainsKey(entry.Key))
                    {
                        aggregateSummary[entry.Key] += entry.Value;
                    }
                    else
                    {
                        aggregateSummary.Add(entry.Key, entry.Value);
                    }
                }

                this.ActivitySummaries = aggregateSummary;
                OnPropertyChanged("ActivitySummaries");
            }
            else if (e.PropertyName == "CurrentActivityRegion")
            {
                this.CurrentActivityRegion = dailyActivity.CurrentActivityRegion;
                OnPropertyChanged("CurrentActivityRegion");
            }
        }

        private void ActivityRegions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (ActivityRegion item in e.NewItems)
            {
                this.ActivityRegions.Add(item);
            }
        }
    }
}
