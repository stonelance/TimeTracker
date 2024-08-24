using Microsoft.Win32;

namespace TimeTracker.Watchers
{
    public class LockScreenWatcher : BaseWatcher
    {
        public enum State
        {
            Inactive,
            Active,
        }

        public State CurrentState;

        public override bool IsActive
        {
            get
            {
                return this.CurrentState == State.Active;
            }
        }

        public LockScreenWatcher(string displayName, ActivityId activity)
            : base(displayName, activity)
        {
            Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        public override void Cancel()
        {
            base.Cancel();

            Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            State newState = State.Inactive;

            if (e.Reason == SessionSwitchReason.SessionLock ||
                e.Reason == SessionSwitchReason.SessionLogoff ||
                e.Reason == SessionSwitchReason.RemoteDisconnect ||
                e.Reason == SessionSwitchReason.ConsoleDisconnect)
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
