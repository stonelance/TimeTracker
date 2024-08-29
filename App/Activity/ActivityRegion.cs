using System;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Serialization;

namespace TimeTracker
{
    [Serializable]
    public class ActivityRegion : INotifyPropertyChanged
    {
        public DateTime StartTime { get; set; }

        private DateTime endTime;
        public DateTime EndTime
        {
            get
            {
                return this.endTime;
            }
            set
            {
                this.endTime = value;
                OnPropertyChanged("EndTime");
                OnPropertyChanged("Duration");
                OnPropertyChanged("DurationPixelWidth");
            }
        }

        [XmlIgnore]
        public ActivityId ActivityId { get; private set; }

        public string Activity
        {
            get { return ActivityManager.Instance.GetNameFromActivity(this.ActivityId); }
            set { this.ActivityId = ActivityManager.Instance.GetActivityFromName(value); }
        }

        public Color Color
        {
            get
            {
                return ActivityManager.Instance.GetColorFromActivity(this.ActivityId);
            }
        }

        public TimeSpan Duration
        {
            get
            {
                return this.EndTime - this.StartTime;
            }
        }

        public double DurationPixelWidth
        {
            get
            {
                return (this.EndTime - this.StartTime).TotalSeconds;
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

        // For serialization
        public ActivityRegion()
        {
        }

        public ActivityRegion(DateTime startTime, DateTime endTime, ActivityId activity)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.ActivityId = activity;
        }
    }
}
