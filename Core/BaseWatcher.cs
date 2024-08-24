using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace TimeTracker.Watchers
{
    public class BaseWatcher : INotifyPropertyChanged
    {
        public ActivityId Activity;
        public string DisplayName;

        protected CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public BaseWatcher(string displayName, ActivityId activity)
        {
            this.DisplayName = displayName;
            this.Activity = activity;
        }

        public virtual void Cancel()
        {
            this.cancellationTokenSource.Cancel();
        }

        public virtual bool IsActive { get { return false; } }

        public virtual void OnForegroundProcessNameChanged(string foregroundProcessName) { }
    }
}
