using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTracker.ViewModel
{
    public class DailyActivityVM
    {
        private DailyActivityAggregate dailyActivity;

        public ObservableCollection<ActivityRegion> ActivityRegions
        {
            get
            {
                return this.dailyActivity.ActivityRegions;
            }
        }

        public List<ActivityVM> ActivitySummaries { get; private set; }

        public bool IsShowingOvernightAway
        {
            get
            {
                return this.dailyActivity.IsShowingOvernightAway;
            }
            set
            {
                this.dailyActivity.IsShowingOvernightAway = value;
            }
        }

        public DailyActivityVM(DailyActivityAggregate dailyActivity)
        {
            this.dailyActivity = dailyActivity;
            this.ActivitySummaries = this.dailyActivity.ActivitySummaries.Select(x => new ActivityVM(this.dailyActivity, x.Key)).ToList();
        }
    }
}
