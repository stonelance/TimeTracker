using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TimeTracker.Watchers
{
    public class ProcessFocusWatcher : BaseWatcher
    {
        public enum State
        {
            Inactive,
            Active,
        }

        private string processName;

        public State CurrentState;

        public ProcessFocusWatcher(string displayName, ActivityId activity, string processName)
            : base(displayName, activity)
        {
            this.processName = processName;
        }

		public void OnForegroundProcessNameChanged(string foregroundProcessName)
		{
			State newState = State.Inactive;

			if (String.Equals(foregroundProcessName, this.processName, StringComparison.OrdinalIgnoreCase))
			{
				newState = State.Active;
			}

			if (this.CurrentState != newState)
			{
				this.CurrentState = newState;
				OnPropertyChanged("");
			}
		}
    }
}
