using System.ComponentModel;
using TimeTracker.Watchers;

namespace TimeTracker.ViewModel
{
    public class WatcherVM : INotifyPropertyChanged
    {
        private BaseWatcher watcher;

        public ActivityId Activity { get { return this.watcher.Activity; } }
        public string DisplayName { get { return this.watcher.DisplayName; } }

        private bool active;
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                if (value == active)
                {
                    return;
                }

                active = value;
                OnPropertyChanged("Active");
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

        public WatcherVM(BaseWatcher watcher)
        {
            this.watcher = watcher;
            this.active = false;
        }
    }
}
