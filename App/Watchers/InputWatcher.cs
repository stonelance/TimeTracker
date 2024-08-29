using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TimeTracker.Watchers
{
    public class InputWatcher : BaseWatcher
    {
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int smIndex);

        int SM_REMOTESESSION = 0x1000;

        public enum State
        {
            Inactive,
            Active,
        }

        private float timeToIdleInSeconds;
        private Task watchTask;
        private int updatePeriodInMilliseconds;
        private ActivityId normalActivity;

        public State CurrentState;

        public override bool IsActive
        {
            get
            {
                // Intentionally backwards, because the UI uses this to determine whether to mark active the associated activity (for idling)
                return this.CurrentState != State.Active;
            }
        }

        public InputWatcher(string displayName, ActivityId activity, JObject settings)
            : base(displayName, activity, settings)
        {
            this.normalActivity = activity;
            this.timeToIdleInSeconds = settings.Value<float>("TimeToIdleInSeconds");
            this.updatePeriodInMilliseconds = (int)(1000 * settings.Value<double>("UpdatePeriodInSeconds"));

            this.watchTask = Task.Run(async () => { await Watch(this.cancellationTokenSource.Token); }, this.cancellationTokenSource.Token);
        }

        private async Task Watch(CancellationToken cancellationToken)
        {
            NativeMethods.POINT lastPoint = new NativeMethods.POINT();
            NativeMethods.GetCursorPos(out lastPoint);

            DateTime lastActiveTime = DateTime.Now;

            while (!cancellationToken.IsCancellationRequested)
            {
                NativeMethods.POINT currentPoint = new NativeMethods.POINT();
                NativeMethods.GetCursorPos(out currentPoint);

                if (currentPoint.X != lastPoint.X ||
                    currentPoint.Y != lastPoint.Y)
                {
                    lastActiveTime = DateTime.Now;
                }

                State newState = State.Inactive;
                if ((DateTime.Now - lastActiveTime).TotalSeconds < timeToIdleInSeconds)
                {
                    newState = State.Active;
                }

                if (this.CurrentState != newState)
                {
                    this.Activity = GetSystemMetrics(SM_REMOTESESSION) != 0 ? ActivityId.Away : normalActivity;

                    this.CurrentState = newState;
                    OnPropertyChanged("");
                }

                lastPoint = currentPoint;

                try
                {
                    await Task.Delay(this.updatePeriodInMilliseconds, cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                }
            }

            // TODO: add logic for game controller input?
        }
    }
}
