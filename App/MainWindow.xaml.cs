using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TimeTracker.ViewModel;

namespace TimeTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private DateTime startTime;
        public DateTime StartTime
        {
            get
            {
                return this.startTime;
            }
            private set
            {
                if (this.startTime == value)
                {
                    return;
                }

                this.startTime = value;
                OnPropertyChanged("StartTime");
            }
        }

        private DateTime endTime;
        public DateTime EndTime
        {
            get
            {
                return this.endTime;
            }
            private set
            {
                if (this.endTime == value)
                {
                    return;
                }

                this.endTime = value;
                OnPropertyChanged("EndTime");
            }
        }

        private DailyActivityAggregate aggragateDailyActivity = new DailyActivityAggregate();

        public DailyActivityVM selectedActivity;
        public DailyActivityVM SelectedActivity
        {
            get
            {
                return this.selectedActivity;
            }
            private set
            {
                if (this.selectedActivity == value)
                {
                    return;
                }

                this.selectedActivity = value;
                OnPropertyChanged("SelectedActivity");
            }
        }

        public ActivityTracker ActivityTracker { get; private set; }
        public DailyActivity TodaysActivity { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task updateTask;
        private Task saveTask;
        private Configuration configuration;

        public MainWindow()
        {
			/*
            this.configuration = new Configuration();

            // Focus based watchers
            this.configuration.Activities.Add(new Configuration.ActivityConfig() { Name = "Surfing", Color = Colors.Cyan });
            this.configuration.Activities.Add(new Configuration.ActivityConfig() { Name = "RemoteDesktop", Color = Colors.Green });
            this.configuration.Activities.Add(new Configuration.ActivityConfig() { Name = "Email", Color = Colors.Blue });
            this.configuration.Activities.Add(new Configuration.ActivityConfig() { Name = "Chat", Color = Colors.Beige });
            this.configuration.Activities.Add(new Configuration.ActivityConfig() { Name = "Programming", Color = Colors.Yellow });

            // Idle
            this.configuration.Activities.Add(new Configuration.ActivityConfig() { Name = "Idle", Color = Colors.Pink });

            // Activity based watchers
            this.configuration.Activities.Add(new Configuration.ActivityConfig() { Name = "Loading", Color = Colors.Orange });
            this.configuration.Activities.Add(new Configuration.ActivityConfig() { Name = "Linking", Color = Colors.Aquamarine });
            this.configuration.Activities.Add(new Configuration.ActivityConfig() { Name = "Compiling", Color = Colors.Purple });

            this.configuration.ProcessFocusWatchers.Add(new Configuration.ProcessFocusWatcherConfig("Outlook", "Email", "outlook", 0.5));
            this.configuration.ProcessFocusWatchers.Add(new Configuration.ProcessFocusWatcherConfig("Skype", "Chat", "lync", 0.5));
            this.configuration.ProcessFocusWatchers.Add(new Configuration.ProcessFocusWatcherConfig("Remote Desktop", "RemoteDesktop", "mstsc", 0.5));
            this.configuration.ProcessFocusWatchers.Add(new Configuration.ProcessFocusWatcherConfig("Internet Explorer", "Surfing", "iexplore", 0.5));
            this.configuration.ProcessFocusWatchers.Add(new Configuration.ProcessFocusWatcherConfig("Chrome", "Surfing", "chrome", 0.5));
            this.configuration.ProcessFocusWatchers.Add(new Configuration.ProcessFocusWatcherConfig("Visual Studio", "Programming", "devenv", 0.5));

            this.configuration.ProcessActivityWatchers.Add(new Configuration.ProcessActivityWatcherConfig("Compiler", "Compiling", "cl", 0.02, 0, 0.5));
            this.configuration.ProcessActivityWatchers.Add(new Configuration.ProcessActivityWatcherConfig("Linker", "Linking", "link", 0.02, 0, 0.5));

			var jsonConfiguration = JsonConvert.SerializeObject(this.configuration, Formatting.Indented);
			File.WriteAllText(@"E:\defaultConfiguration.json", jsonConfiguration);
			*/

			var configurationPath = Path.Combine(GetAppDataPath(), "configuration.json");
			if (!File.Exists(configurationPath))
			{
				var defaultConfigurationpath = "defaultConfiguration.json";
				File.Copy(defaultConfigurationpath, configurationPath, true);
			}

			this.configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configurationPath));

			ActivityManager.Instance.Initialize(this.configuration);

            InitializeTodaysActivity();
            this.ActivityTracker = new ActivityTracker(this.configuration, this.TodaysActivity);

            var startDate = DateTime.Now.Date;
            SetDateRange(startDate, startDate.AddDays(1).AddTicks(-1));

            this.DataContext = this;
            this.SelectedActivity = new DailyActivityVM(this.aggragateDailyActivity);

            InitializeComponent();

            // Queue up an extra auto fit once we have rendered once
            this.Dispatcher.BeginInvoke(new Action(() => { AutoFitTimeline(); }), DispatcherPriority.Render, null);

            this.updateTask = Task.Run(async () =>
                {
                    while (!this.cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        lock (this)
                        {
                            DateTime now = DateTime.Now;

                            if (now.Date != this.TodaysActivity.StartTime.Date)
                            {
                                bool updateDateRangeStart = this.StartTime.Date == this.TodaysActivity.StartTime.Date;
                                bool updateDateRangeEnd = this.EndTime.Date == this.TodaysActivity.EndTime.Date;

                                // Get the end of the day time
                                var currentDaysActivity = this.TodaysActivity;
                                var endOfDay = this.TodaysActivity.StartTime.Date.AddDays(1);

                                // Create a new days activity, setting it should update the current activity to the end of the day.
                                this.TodaysActivity = new DailyActivity(endOfDay);
                                this.ActivityTracker.SetDailyActivity(this.TodaysActivity, endOfDay);
                                Save(currentDaysActivity);
                                this.ActivityTracker.Update(now);

                                if (updateDateRangeStart || updateDateRangeEnd)
                                {
                                    // Current range includes today, so increment
                                    var newStart = updateDateRangeStart ? this.StartTime.Date.AddDays(1) : this.StartTime;
                                    var newEnd = updateDateRangeEnd ? this.EndTime.Date.AddDays(1) : this.EndTime;
                                    SetDateRange(newStart, newEnd);
                                }
                            }
                            else
                            {
                                this.ActivityTracker.Update(now);
                            }
                        }

                        try
                        {
                            await Task.Delay(200, this.cancellationTokenSource.Token);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                    }
                }, this.cancellationTokenSource.Token);

            this.saveTask = Task.Run(async () =>
                {
                    while (!this.cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        lock (this)
                        {
                            Save(this.TodaysActivity);
                        }

                        try
                        {
                            await Task.Delay(60 * 1000, this.cancellationTokenSource.Token);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                    }
                }, this.cancellationTokenSource.Token);
        }

		private string GetAppDataPath()
		{
			var rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Process.GetCurrentProcess().ProcessName.Replace(".vshost", ""));
			if (!Directory.Exists(rootPath))
			{
				Directory.CreateDirectory(rootPath);
			}

			return rootPath;
		}

		private string GetPathForDate(DateTime date)
        {
            var rootPath = GetAppDataPath();
            return Path.Combine(rootPath, date.ToString("yy-MM-dd") + ".xml");
        }

        private void InitializeTodaysActivity()
        {
            var path = GetPathForDate(DateTime.Now);
            if (File.Exists(path))
            {
                try
                {
                    this.TodaysActivity = DailyActivity.Deserialize(path);
                }
                catch (Exception)
                {
                }
            }

            if (this.TodaysActivity == null)
            {
                this.TodaysActivity = new DailyActivity();
            }
        }

        private void SetDateRange(DateTime start, DateTime end)
        {
            start = start.Date;
            end = end.Date.AddDays(1).AddTicks(-1);
            var now = DateTime.Now;

            if (end.Date > now.Date)
            {
                end = now.Date.AddDays(1).AddTicks(-1);
            }

            if (start > end)
            {
                start = end;
            }


            if (this.StartTime == start && this.EndTime == end)
            {
                return;
            }

            this.StartTime = start.Date;
            this.EndTime = end.Date.AddDays(1).AddTicks(-1);

            List<DailyActivity> dailyActivities = new List<DailyActivity>();

            // Don't include today in the list of things to load because we already have it loaded
            var endDate = this.EndTime;
            if (endDate.Date == now.Date)
            {
                endDate = endDate.AddDays(-1);
            }
            for (var currentDate = this.StartTime.Date; currentDate <= endDate; currentDate = currentDate.AddDays(1))
            {
                var path = GetPathForDate(currentDate);
                if (File.Exists(path))
                {
                    try
                    {
                        dailyActivities.Add(DailyActivity.Deserialize(path));
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            // Add today's data if needed
            if (this.EndTime.Date == now.Date)
            {
                dailyActivities.Add(this.TodaysActivity);
            }

            this.aggragateDailyActivity.SetDailyActivities(dailyActivities);

            this.Dispatcher.Invoke(() =>
                {
                    UpdateLayout();
                    AutoFitTimeline();
                });
        }

        private void Save(DailyActivity dailyActivity)
        {
            var path = GetPathForDate(dailyActivity.StartTime.Date);

            try
            {
                dailyActivity.Serialize(path);
            }
            catch (InvalidOperationException e)
            {
                // Ignore these, as we will just try to save again in a minute or so
                // TODO: if we detect multiple failures in a row, message the user?
                if (!(e.InnerException is IOException))
                {
                    throw e;
                }
            }
            catch (IOException)
            {
                // Ignore these, as we will just try to save again in a minute or so
                // TODO: if we detect multiple failures in a row, message the user?
            }
        }

        private void AutoFitTimeline()
        {
            if (this.timelineViewer != null)
            {
                var targetScaledWidth = this.timelineViewer.RenderSize.Width * 0.9; // Always leave a little room for new content
                var unscaledWidth = this.timelineViewer.ExtentWidth / this.scaleTransform.ScaleX;
                var newScale = Math.Min(1, Math.Max(0.00001, targetScaledWidth / (unscaledWidth + 1))); // Prevent div by 0 by adding one

                this.scaleTransform.ScaleX = newScale;
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var unscaledWidth = this.timelineViewer.ExtentWidth / this.scaleTransform.ScaleX;
                var minScaledWidth = this.timelineViewer.RenderSize.Width * 0.05; // Don't scale the timeline smaller than this width
                var minScale = Math.Max(0.00001, minScaledWidth / (unscaledWidth + 1)); // Prevent div by 0 by adding one

                double scaleChange = 1.0 + e.Delta / 600.0;

                var mousePositionInTimeline = e.GetPosition(this.timelineViewer).X;
                var mousePositionInUnscaledTimeline = (this.timelineViewer.HorizontalOffset + mousePositionInTimeline) / this.scaleTransform.ScaleX;

                this.scaleTransform.ScaleX = Math.Max(minScale, this.scaleTransform.ScaleX * scaleChange);

                this.timelineViewer.ScrollToHorizontalOffset(mousePositionInUnscaledTimeline * this.scaleTransform.ScaleX - mousePositionInTimeline);
            }
        }

        private void Button_StartDate_Click(object sender, RoutedEventArgs e)
        {
            var window = new DateRangeWindow(this.StartTime, this.StartTime, false);
            window.Owner = this;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                if (window.Start > this.EndTime)
                {
                    SetDateRange(window.Start, window.Start);
                }
                else
                {
                    SetDateRange(window.Start, this.EndTime);
                }
            }
        }

        private void Button_EndDate_Click(object sender, RoutedEventArgs e)
        {
            var window = new DateRangeWindow(this.EndTime, this.EndTime, false);
            window.Owner = this;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                if (window.End < this.StartTime.Date)
                {
                    SetDateRange(window.End, window.End);
                }
                else
                {
                    SetDateRange(this.StartTime, window.End);
                }
            }
        }

        private void Button_DateRange_Click(object sender, RoutedEventArgs e)
        {
            var window = new DateRangeWindow(this.StartTime, this.EndTime, true);
            window.Owner = this;
            var result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                SetDateRange(window.Start, window.End);
            }
        }

        private void DataGrid_AutoGeneratingColumn(object sender, System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs e)
        {
            if (((PropertyDescriptor)e.PropertyDescriptor).IsBrowsable == false)
            {
                e.Cancel = true;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            lock (this)
            {
                this.cancellationTokenSource.Cancel();
                this.updateTask.Wait();
                this.saveTask.Wait();
                this.ActivityTracker.Cancel();

                Save(this.TodaysActivity);
            }
        }
    }
}
