﻿using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TimeTracker.ViewModel
{
    public class ActivityVM : INotifyPropertyChanged
    {
        private ActivityId Activity { get; set; }

        public string Name { get { return ActivityManager.Instance.GetNameFromActivity(this.Activity); } }

        public bool Active
        {
            get
            {
                var currentActivityRegion = this.dailyActivity.CurrentActivityRegion;
                return currentActivityRegion != null ? currentActivityRegion.ActivityId == this.Activity : false;
            }
        }

        [Browsable(false)]
        public Rectangle Color
        {
            get
            {
                var color = ActivityManager.Instance.GetColorFromActivity(this.Activity);
                return new Rectangle() { Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B)) };
            }
        }

        public TimeSpan TotalActiveTime
        {
            get
            {
                return this.dailyActivity.ActivitySummaries[this.Activity];
            }
        }

        public string RelativeActiveTime
        {
            get
            {
                if (!ActivityManager.Instance.IsIncludedInRelativeTime(this.Activity))
                {
                    return "N/A";
                }

                TimeSpan totalTime = new TimeSpan();
                foreach (var entry in this.dailyActivity.ActivitySummaries)
                {
                    if (ActivityManager.Instance.IsIncludedInRelativeTime(entry.Key))
                    {
                        totalTime += entry.Value;
                    }
                }
                var relativeTime = this.dailyActivity.ActivitySummaries[this.Activity].TotalSeconds / totalTime.TotalSeconds;
                return String.Format("{0:0.00}%", 100 * relativeTime);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        private DailyActivityAggregate dailyActivity;

        public ActivityVM(DailyActivityAggregate dailyActivity, ActivityId activity)
        {
            this.dailyActivity = dailyActivity;
            this.dailyActivity.PropertyChanged += dailyActivity_PropertyChanged;
            this.Activity = activity;
        }

        private void dailyActivity_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ActivitySummaries":
                    OnPropertyChanged("TotalActiveTime");
                    OnPropertyChanged("RelativeActiveTime");
                    break;

                case "CurrentActivityRegion":
                    OnPropertyChanged("Active");
                    break;
            }
        }
    }
}
