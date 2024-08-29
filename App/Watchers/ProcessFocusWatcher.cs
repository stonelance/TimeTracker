using Newtonsoft.Json.Linq;
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

        public override bool IsActive
        {
            get
            {
                return this.CurrentState == State.Active;
            }
        }

        public ProcessFocusWatcher(string displayName, ActivityId activity, JObject settings)
            : base(displayName, activity, settings)
        {
            this.processName = settings.Value<string>("ProcessName");
        }

        public ProcessFocusWatcher(string displayName, ActivityId activity, string processName)
            : base(displayName, activity, null)
        {
            this.processName = processName;
        }

		public override void OnForegroundProcessNameChanged(string foregroundProcessName)
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
