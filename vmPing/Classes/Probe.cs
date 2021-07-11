using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using vmPing.Properties;
using vmPing.Views;

namespace vmPing.Classes
{
    public enum ProbeStatus
    {
        Up,
        Down,
        Error,
        Indeterminate,
        Inactive,
        Scanner,
        Start,
        Stop
    }


    public enum ProbeType
    {
        Dns,
        Ping,
        Traceroute,
        Comment
    }


    public partial class Probe : INotifyPropertyChanged
    {
        public static ObservableCollection<StatusChangeLog> StatusChangeLog = new ObservableCollection<StatusChangeLog>();
        public static StatusHistoryWindow StatusWindow;

        private static Mutex mutex = new Mutex();
        private static int activeCount;
        public static int ActiveCount
        {
            get => activeCount;
            set
            {
                activeCount = value;
                OnActiveCountChanged(EventArgs.Empty);
            }
        }
        public static event EventHandler ActiveCountChanged;
        protected static void OnActiveCountChanged(EventArgs e) => ActiveCountChanged?.Invoke(null, e);
        public event PropertyChangedEventHandler PropertyChanged;

        public IsolatedPingWindow IsolatedWindow { get; set; }
        public int IndeterminateCount { get; set; }
        public PingStatistics Statistics { get; set; } = new PingStatistics();
        public int SelStart { get; set; }
        public int SelLength { get; set; }
        public CancellationTokenSource CancelSource { get; set; }
        private ObservableCollection<string> history;
        public ObservableCollection<string> History
        {
            get => history;
            set
            {
                history = value;
                history.CollectionChanged += History_CollectionChanged;
                NotifyPropertyChanged("History");
            }
        }
        public string HistoryAsString
        {
            get
            {
                return History != null ? string.Join(Environment.NewLine, History) : string.Empty;
            }
        }
        void History_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("HistoryAsString");
        }

        private ProbeType type = ProbeType.Ping;
        public ProbeType Type
        {
            get => type;
            set
            {
                type = value;
                NotifyPropertyChanged("Type");
            }
        }

        private string hostname;
        public string Hostname
        {
            get => hostname;
            set
            {
                if (value != hostname)
                {
                    hostname = value.Trim();
                    NotifyPropertyChanged("Hostname");
                }
            }
        }

        private string alias;
        public string Alias
        {
            get => alias;
            set
            {
                alias = value;
                NotifyPropertyChanged("Alias");
            }
        }

        private ProbeStatus status = ProbeStatus.Inactive;
        public ProbeStatus Status
        {
            get => status;
            set
            {
                status = value;
                NotifyPropertyChanged("Status");
            }
        }

        private bool isActive = false;
        public bool IsActive
        {
            get => isActive;
            set
            {
                if (value != isActive)
                {
                    isActive = value;
                    NotifyPropertyChanged("IsActive");

                    mutex.WaitOne();
                    if (value == true)
                        ++ActiveCount;
                    else
                        --ActiveCount;
                    mutex.ReleaseMutex();
                    NotifyPropertyChanged("NumberOfActivePings");
                }
            }
        }

        private string statisticsText;
        public string StatisticsText
        {
            get => statisticsText;
            set
            {
                if (value != statisticsText)
                {
                    statisticsText = value;
                    NotifyPropertyChanged("StatisticsText");
                }
            }
        }


        public void AddHistory(string historyItem)
        {
            const int MaxSize = 3600;

            History.Add(historyItem);
            if (History.Count > MaxSize)
                History.RemoveAt(0);
        }

        public void WriteFinalStatisticsToHistory()
        {
            if (Statistics == null || Statistics.Sent == 0) return;

            var roundTripTimes = new List<int>();
            var rttRegex = new Regex($@"  \[(?<rtt><?\d+) ?{Strings.Milliseconds_Symbol}]$");

            foreach (var historyItem in History)
            {
                Match regexMatch = rttRegex.Match(historyItem);
                if (!regexMatch.Success) continue;
                if (regexMatch.Groups["rtt"].Value == "<1")
                    roundTripTimes.Add(0);
                else
                    roundTripTimes.Add(int.Parse(regexMatch.Groups["rtt"].Value));
            }

            // Display stats and round trip times.
            AddHistory("");
            AddHistory(
                $"Sent {Statistics.Sent}, " +
                $"Received {Statistics.Received}, " +
                $"Lost {Statistics.Sent - Statistics.Received} ({(100 * (Statistics.Sent - Statistics.Received)) / Statistics.Sent}% loss)");
            if (roundTripTimes.Count > 0)
            {
                AddHistory(
                    $"Minimum ({roundTripTimes.Min()}{Strings.Milliseconds_Symbol}), " +
                    $"Maximum ({roundTripTimes.Max()}{Strings.Milliseconds_Symbol}), " +
                    $"Average ({roundTripTimes.Average().ToString("0.##")}{Strings.Milliseconds_Symbol})");
            }
            AddHistory(" ");
        }

        private void NotifyPropertyChanged(string info) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
    }
}
