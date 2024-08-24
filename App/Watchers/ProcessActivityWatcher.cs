using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TimeTracker.Watchers
{
    public class ProcessActivityWatcher : BaseWatcher
    {
        public enum State
        {
            NotRunning = 0,
            Idle,
            Running,
        }

        private string processName;
        private double cpuUsageThresholdForRunning;
        private double delayBeforeReturnToInactiveInSeconds;
        private int updatePeriodInMilliseconds;
        private Task watchTask;

        public State CurrentState;


        public event EventHandler ProcessStarted;
        public event EventHandler ProcessActive;
        public event EventHandler ProcessInactive;
        public event EventHandler ProcessStopped;

        public override bool IsActive
        {
            get
            {
                return this.CurrentState == State.Running;
            }
        }

        public ProcessActivityWatcher(string displayName, ActivityId activity, string processName, double cpuUsageThresholdForRunning, double delayBeforeReturnToInactiveInSeconds, double updatePeriodInSeconds)
            : base(displayName, activity)
        {
            this.processName = processName;
            this.cpuUsageThresholdForRunning = cpuUsageThresholdForRunning;
            this.delayBeforeReturnToInactiveInSeconds = delayBeforeReturnToInactiveInSeconds;
            this.updatePeriodInMilliseconds = (int)(updatePeriodInSeconds * 1000);

            this.watchTask = Task.Run(async () => { await Watch(this.cancellationTokenSource.Token); }, this.cancellationTokenSource.Token);
        }

        private async Task Watch(CancellationToken cancellationToken)
        {
            Dictionary<int, TimeSpan> processIdToLastProcessorTime = new Dictionary<int, TimeSpan>();

            bool firstPass = true;

            DateTime lastUpdate = DateTime.Now;
            DateTime lastActiveTime = DateTime.Now;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }

                DateTime thisUpdate = DateTime.Now;
                State newState = State.NotRunning;
                try
                {
                    var processes = Process.GetProcessesByName(processName).Where(x => !x.HasExited).ToDictionary(x => x.Id, x => x);

                    TimeSpan updateDelta = thisUpdate - lastUpdate;
                    lastUpdate = thisUpdate;

                    if (processes.Count > 0)
                    {
                        TimeSpan totalProcessorTime = new TimeSpan();
                        foreach (var entry in processes)
                        {
                            TimeSpan processorTime;
                            try
                            {
                                processorTime = entry.Value.TotalProcessorTime;
                            }
                            catch (Exception)
                            {
                                // This can happen if the process exits as we are looping
                                continue;
                            }

                            if (!processIdToLastProcessorTime.ContainsKey(entry.Key))
                            {
                                processIdToLastProcessorTime.Add(entry.Key, entry.Value.TotalProcessorTime);

                                if (!firstPass && processorTime.TotalSeconds > 0)
                                {
                                    totalProcessorTime += processorTime;
                                }
                            }
                            else
                            {
                                if ((processorTime - processIdToLastProcessorTime[entry.Key]).TotalSeconds > 0)
                                {
                                    totalProcessorTime += processorTime - processIdToLastProcessorTime[entry.Key];
                                }
                                processIdToLastProcessorTime[entry.Key] = processorTime;
                            }
                        }

                        var entriesToDelete = processIdToLastProcessorTime.Where(x => !processes.ContainsKey(x.Key)).Select(x => x.Key).ToList();
                        foreach (var entry in entriesToDelete)
                        {
                            processIdToLastProcessorTime.Remove(entry);
                        }

                        // Get current CPU usage and scale to 0-1
                        newState = State.Idle;
                        if ((totalProcessorTime.TotalSeconds / updateDelta.TotalSeconds) > this.cpuUsageThresholdForRunning)
                        {
                            lastActiveTime = thisUpdate;
                            newState = State.Running;
                        }
                    }
                }
                catch (Exception)
                {
                    // This can happen if the process exits but there is still a Process object for it (From HasExited)
                }

                if (this.CurrentState != newState)
                {
                    switch (newState)
                    {
                        case State.NotRunning:
                            if (this.ProcessStopped != null)
                            {
                                this.ProcessStopped(this, EventArgs.Empty);
                            }
                            break;

                        case State.Idle:
                            if (this.CurrentState == State.NotRunning)
                            {
                                if (this.ProcessStarted != null)
                                {
                                    this.ProcessStarted(this, EventArgs.Empty);
                                }
                            }
                            else if (this.CurrentState != State.Running || (thisUpdate - lastActiveTime).TotalSeconds > this.delayBeforeReturnToInactiveInSeconds)
                            {
                                if (this.ProcessInactive != null)
                                {
                                    this.ProcessInactive(this, EventArgs.Empty);
                                }
                            }
                            else
                            {
                                newState = this.CurrentState;
                            }
                            break;

                        case State.Running:
                            if (this.CurrentState == State.NotRunning)
                            {
                                if (this.ProcessStarted != null)
                                {
                                    this.ProcessStarted(this, EventArgs.Empty);
                                }
                            }
                            else
                            {
                                if (this.ProcessActive != null)
                                {
                                    this.ProcessActive(this, EventArgs.Empty);
                                }
                            }
                            break;
                    }

                    this.CurrentState = newState;
                    OnPropertyChanged("");
                }

                try
                {
                    await Task.Delay(this.updatePeriodInMilliseconds, cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                }

                firstPass = false;
            }
        }
    }
}
