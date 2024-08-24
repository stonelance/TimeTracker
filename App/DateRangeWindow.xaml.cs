using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TimeTracker
{
    /// <summary>
    /// Interaction logic for TimeRangeSelection.xaml
    /// </summary>
    public partial class DateRangeWindow : Window
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        public SelectionMode SelectionMode { get; private set; }

        public DateRangeWindow(DateTime start, DateTime end, bool multiDateSelection)
        {
            this.Start = start.Date;
            this.End = end.Date;
            this.SelectionMode = multiDateSelection ? SelectionMode.Multiple : SelectionMode.Single;

            InitializeComponent();
            this.calendar.SelectedDates.AddRange(this.Start, this.End);
        }

        private void calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Start = this.calendar.SelectedDates[0].Date;
            this.End = this.calendar.SelectedDates[this.calendar.SelectedDates.Count - 1].Date;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
