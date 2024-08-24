using System;
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
                return new Rectangle() { Fill = new SolidColorBrush(ActivityManager.Instance.GetColorFromActivity(this.Activity)) };
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
                if (this.Activity == ActivityId.NoData ||
                    this.Activity == ActivityId.Away)
                {
                    return "N/A";
                }

                TimeSpan totalTime = new TimeSpan();
                foreach (var entry in this.dailyActivity.ActivitySummaries)
                {
                    if (entry.Key != ActivityId.NoData &&
                        entry.Key != ActivityId.Away)
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
