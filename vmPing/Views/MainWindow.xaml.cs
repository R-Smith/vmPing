﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using vmPing.Classes;
using System.Timers;
using System.Net;

namespace vmPing.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<PingItem> _pingItems = new ObservableCollection<PingItem>();
        private ObservableCollection<StatusChangeLog> _statusChangeLog = new ObservableCollection<StatusChangeLog>();

        public static RoutedCommand AlwaysOnTopCommand = new RoutedCommand();
        public static RoutedCommand ProbeOptionsCommand = new RoutedCommand();
        public static RoutedCommand LogOutputCommand = new RoutedCommand();
        public static RoutedCommand EmailAlertsCommand = new RoutedCommand();
        public static RoutedCommand StartStopCommand = new RoutedCommand();
        public static RoutedCommand HelpCommand = new RoutedCommand();
        public static RoutedCommand NewInstanceCommand = new RoutedCommand();
        public static RoutedCommand TraceRouteCommand = new RoutedCommand();
        public static RoutedCommand FloodHostCommand = new RoutedCommand();
        public static RoutedCommand AddMonitorCommand = new RoutedCommand();


        public MainWindow()
        {
            InitializeComponent();
            InitializeAplication();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial focus first text box.
            if (_pingItems.Count > 0)
            {
                var cp = icPingItems.ItemContainerGenerator.ContainerFromIndex(0) as ContentPresenter;
                var tb = (TextBox)cp.ContentTemplate.FindName("tbHostname", cp);

                if (tb != null)
                    tb.Focus();
            }
        }


        private void InitializeAplication()
        {
            InitializeCommandBindings();
            ParseCommandLineArguments();
            RefreshFavorites();
            RefreshApplicationsOptions();

            sliderColumns.Value = _pingItems.Count;
            icPingItems.ItemsSource = _pingItems;
        }


        private void InitializeCommandBindings()
        {
            CommandBindings.Add(new CommandBinding(AlwaysOnTopCommand, AlwaysOnTopExecute));
            CommandBindings.Add(new CommandBinding(ProbeOptionsCommand, ProbeOptionsExecute));
            CommandBindings.Add(new CommandBinding(LogOutputCommand, LogOutputExecute));
            CommandBindings.Add(new CommandBinding(EmailAlertsCommand, EmailAlertsExecute));
            CommandBindings.Add(new CommandBinding(StartStopCommand, StartStopExecute));
            CommandBindings.Add(new CommandBinding(HelpCommand, HelpExecute));
            CommandBindings.Add(new CommandBinding(NewInstanceCommand, NewInstanceExecute));
            CommandBindings.Add(new CommandBinding(TraceRouteCommand, TraceRouteExecute));
            CommandBindings.Add(new CommandBinding(FloodHostCommand, FloodHostExecute));
            CommandBindings.Add(new CommandBinding(AddMonitorCommand, AddMonitorExecute));

            var kgAlwaysOnTop = new KeyGesture(Key.F9);
            var kgProbeOptions = new KeyGesture(Key.F10);
            var kgLogOutput = new KeyGesture(Key.F11);
            var kgEmailAlerts = new KeyGesture(Key.F12);
            var kgStartStop = new KeyGesture(Key.F5);
            var kgHelp = new KeyGesture(Key.F1);
            var kgNewInstance = new KeyGesture(Key.N, ModifierKeys.Control);
            var kgTraceRoute = new KeyGesture(Key.T, ModifierKeys.Control);
            var kgFloodHost = new KeyGesture(Key.F, ModifierKeys.Control);
            var kgAddMonitor = new KeyGesture(Key.A, ModifierKeys.Control);
            InputBindings.Add(new InputBinding(AlwaysOnTopCommand, kgAlwaysOnTop));
            InputBindings.Add(new InputBinding(ProbeOptionsCommand, kgProbeOptions));
            InputBindings.Add(new InputBinding(LogOutputCommand, kgLogOutput));
            InputBindings.Add(new InputBinding(EmailAlertsCommand, kgEmailAlerts));
            InputBindings.Add(new InputBinding(StartStopCommand, kgStartStop));
            InputBindings.Add(new InputBinding(HelpCommand, kgHelp));
            InputBindings.Add(new InputBinding(NewInstanceCommand, kgNewInstance));
            InputBindings.Add(new InputBinding(TraceRouteCommand, kgTraceRoute));
            InputBindings.Add(new InputBinding(FloodHostCommand, kgFloodHost));
            InputBindings.Add(new InputBinding(AddMonitorCommand, kgAddMonitor));

            StartStopMenu.Command = StartStopCommand;
            HelpMenu.Command = HelpCommand;
            NewInstanceMenu.Command = NewInstanceCommand;
            TraceRouteMenu.Command = TraceRouteCommand;
            FloodHostMenu.Command = FloodHostCommand;
            AddMonitorMenu.Command = AddMonitorCommand;
        }


        private void ParseCommandLineArguments()
        {
            var commandLineArgs = Environment.GetCommandLineArgs();
            var errorString = string.Empty;
            var hostnameList = new List<string>();

            for (var index = 1; index < commandLineArgs.Length; ++index)
            {
                int numValue;

                switch (commandLineArgs[index].ToLower())
                {
                    case "/i":
                    case "-i":
                        if (index + 1 < commandLineArgs.Length &&
                            int.TryParse(commandLineArgs[index + 1], out numValue) &&
                            numValue > 0 && numValue <= 86400)
                        {
                            ApplicationOptions.PingInterval = numValue * 1000;
                            ++index;
                        }
                        else
                        {
                            errorString += $"For switch -i you must specify the number of seconds between 1 and 86400.{Environment.NewLine}";
                            break;
                        }
                        break;
                    case "/w":
                    case "-w":
                        if (commandLineArgs.Length > index + 1 &&
                            int.TryParse(commandLineArgs[index + 1], out numValue) &&
                            numValue > 0 && numValue <= 60)
                        {
                            ApplicationOptions.PingTimeout = numValue * 1000;
                            ++index;
                        }
                        else
                        {
                            errorString += $"For switch -w you must specify the number of seconds between 1 and 60.{Environment.NewLine}";
                            break;
                        }
                        break;
                    case "/?":
                    case "-?":
                    case "--help":
                        MessageBox.Show(
                            $"Command Line Usage:{Environment.NewLine}vmPing [-i interval] [-w timeout] [<target_host>...]",
                            "vmPing Help",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        Application.Current.Shutdown();
                        break;
                    default:
                        hostnameList.Add(commandLineArgs[index]);
                        break;
                }
            }
            if (errorString.Length > 0)
            {
                MessageBox.Show(
                    $"{errorString}{Environment.NewLine}{Environment.NewLine}Command Line Usage:{Environment.NewLine}vmPing [-i interval] [-w timeout] [<target_host>...]",
                    "vmPing Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            if (hostnameList.Count > 0)
            {
                AddHostMonitor(hostnameList.Count);
                for (int i = 0; i < hostnameList.Count; ++i)
                {
                    _pingItems[i].Hostname = hostnameList[i].ToUpper();
                    PingStartStop(_pingItems[i]);
                }
            }
            else
                AddHostMonitor(2);
        }


        public void AddHostMonitor(int numberOfHostMonitors)
        {
            for (; numberOfHostMonitors > 0; --numberOfHostMonitors)
                _pingItems.Add(new PingItem());
        }


        public void btnPing_Click(object sender, EventArgs e)
        {
            PingStartStop((PingItem)((Button)sender).DataContext);
        }


        public void PingStartStop(PingItem pingItem)
        {
            if (string.IsNullOrEmpty(pingItem.Hostname)) return;

            if (!pingItem.IsActive)
            {
                pingItem.IsActive = true;

                if (pingItem.PingBackgroundWorker != null)
                    pingItem.PingBackgroundWorker.CancelAsync();

                pingItem.PingStatisticsText = string.Empty;
                pingItem.History = new ObservableCollection<string>();
                pingItem.AddHistory($"*** Pinging {pingItem.Hostname}:");

                pingItem.PingBackgroundWorker = new BackgroundWorker();
                pingItem.PingResetEvent = new AutoResetEvent(false);
                if (pingItem.Hostname.Count(f => f == ':') == 1)
                    pingItem.PingBackgroundWorker.DoWork += new DoWorkEventHandler(backgroundThread_PerformTcpProbe);
                else
                    pingItem.PingBackgroundWorker.DoWork += new DoWorkEventHandler(backgroundThread_PerformIcmpProbe);
                pingItem.PingBackgroundWorker.WorkerSupportsCancellation = true;
                pingItem.PingBackgroundWorker.WorkerReportsProgress = true;
                pingItem.PingBackgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundThread_ProgressChanged);
                pingItem.PingBackgroundWorker.RunWorkerAsync(pingItem);
            }
            else
            {
                pingItem.PingBackgroundWorker.CancelAsync();
                pingItem.PingResetEvent.WaitOne();
                pingItem.Status = PingStatus.Inactive;
                pingItem.IsActive = false;
            }

            RefreshGlobalStartStop();
        }


        private void backgroundThread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (ApplicationOptions.PopupOption == ApplicationOptions.PopupNotificationOption.Always ||
                (ApplicationOptions.PopupOption == ApplicationOptions.PopupNotificationOption.WhenMinimized &&
                this.WindowState == WindowState.Minimized))
            {
                if (!Application.Current.Windows.OfType<PopupNotificationWindow>().Any())
                {
                    _statusChangeLog.Clear();
                    _statusChangeLog.Add(e.UserState as StatusChangeLog);
                    var wnd = new PopupNotificationWindow(_statusChangeLog);
                    wnd.Show();
                }
                else
                {
                    _statusChangeLog.Add(e.UserState as StatusChangeLog);
                }
            }
        }


        public void RefreshGlobalStartStop()
        {
            // Check if any pings are in progress and update the start/stop all toggle accordingly.
            bool isActive = false;
            foreach (PingItem pingItem in _pingItems)
            {
                if (pingItem.IsActive)
                {
                    isActive = true;
                    break;
                }
            }

            if (isActive)
            {
                StartStopMenuHeader.Text = "_Stop All (F5)";
                StartStopMenuImage.Source = new BitmapImage(new Uri(@"/Resources/stopCircle-16.png", UriKind.Relative));
            }
            else
            {
                StartStopMenuHeader.Text = "_Start All (F5)";
                StartStopMenuImage.Source = new BitmapImage(new Uri(@"/Resources/play-16.png", UriKind.Relative));
            }
        }


        public void backgroundThread_PerformIcmpProbe(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;
            var pingItem = e.Argument as PingItem;

            pingItem.Statistics = new PingStatistics();
            var buffer = Encoding.ASCII.GetBytes(Constants.PING_DATA);
            var options = new PingOptions(Constants.PING_TTL, true);

            // Check whether a hostname or an IP address was provided.  If hostname, resolve and print IP.
            var hostnameType = Uri.CheckHostName(pingItem.Hostname);
            if (hostnameType != UriHostNameType.IPv4 && hostnameType != UriHostNameType.IPv6)
            {
                var host = System.Net.Dns.GetHostEntry(pingItem.Hostname);
                if (host.AddressList.Length > 0)
                    Application.Current.Dispatcher.BeginInvoke(
                                new Action(() => pingItem.AddHistory("*** [" + host.AddressList[0].ToString() + "]")));
            }

            using (pingItem.Sender = new Ping())
            {
                while (!backgroundWorker.CancellationPending && pingItem.IsActive)
                {
                    try
                    {
                        pingItem.Reply = pingItem.Sender.Send(pingItem.Hostname, ApplicationOptions.PingTimeout, buffer, options);
                        if (backgroundWorker.CancellationPending || pingItem.IsActive == false)
                        {
                            pingItem.PingResetEvent.Set();
                            return;
                        }

                        ++pingItem.Statistics.PingsSent;
                        if (pingItem.Reply.Status == IPStatus.Success)
                        {
                            // Check for status change.
                            if (pingItem.Status == PingStatus.Down)
                            {
                                backgroundWorker.ReportProgress(
                                    0,
                                    new StatusChangeLog { Timestamp = DateTime.Now, Hostname = pingItem.Hostname, Status = PingStatus.Up });
                                if (ApplicationOptions.EmailAlert)
                                    SendEmail("up", pingItem.Hostname);
                            }

                            pingItem.DownCount = 0;
                            ++pingItem.Statistics.PingsReceived;
                            pingItem.Status = PingStatus.Up;
                        }
                        else 
                        {
                            if (pingItem.Status == PingStatus.Up)
                                pingItem.Status = PingStatus.Indeterminate;
                            if (pingItem.Status == PingStatus.Inactive)
                                pingItem.Status = PingStatus.Down;
                            ++pingItem.DownCount;


                            // Check for status change.
                            if (pingItem.Status == PingStatus.Indeterminate && pingItem.DownCount >= ApplicationOptions.AlertThreshold)
                            {
                                pingItem.Status = PingStatus.Down;
                                backgroundWorker.ReportProgress(
                                    0,
                                    new StatusChangeLog { Timestamp = DateTime.Now, Hostname = pingItem.Hostname, Status = PingStatus.Down });
                                if (ApplicationOptions.EmailAlert)
                                    SendEmail("down", pingItem.Hostname);
                            }

                            if (pingItem.Reply.Status == IPStatus.TimedOut ||
                                pingItem.Reply.Status == IPStatus.DestinationHostUnreachable ||
                                pingItem.Reply.Status == IPStatus.DestinationNetworkUnreachable ||
                                pingItem.Reply.Status == IPStatus.DestinationUnreachable
                                )
                                ++pingItem.Statistics.PingsLost;
                            else
                                ++pingItem.Statistics.PingsError;
                        }

                        DisplayStatistics(pingItem);
                        DisplayIcmpReply(pingItem);
                        pingItem.PingResetEvent.Set();

                        if (pingItem.Reply.Status == IPStatus.TimedOut)
                        {
                            // Ping timed out.  If the ping interval is greater than the timeout,
                            // then sleep for [INTERVAL - TIMEOUT]
                            // Otherwise, sleep for a fixed amount of 1 second
                            if (ApplicationOptions.PingInterval > ApplicationOptions.PingTimeout)
                                Thread.Sleep(ApplicationOptions.PingInterval - ApplicationOptions.PingTimeout);
                            else
                                Thread.Sleep(1000);
                        }
                        else
                            // For any other type of ping response, sleep for the global ping interval amount
                            // before sending another ping.
                            Thread.Sleep(ApplicationOptions.PingInterval);
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException is SocketException)
                            Application.Current.Dispatcher.BeginInvoke(
                                new Action(() => pingItem.AddHistory("Unable to resolve hostname.")));
                        else
                            Application.Current.Dispatcher.BeginInvoke(
                                new Action(() => pingItem.AddHistory("Error: " + ex.Message)));

                        e.Cancel = true;

                        // Check for status change.
                        if (pingItem.Status == PingStatus.Up || pingItem.Status == PingStatus.Down || pingItem.Status == PingStatus.Indeterminate)
                        {
                            backgroundWorker.ReportProgress(
                                0,
                                new StatusChangeLog { Timestamp = DateTime.Now, Hostname = pingItem.Hostname, Status = PingStatus.Error });
                            if (ApplicationOptions.EmailAlert)
                                SendEmail("error", pingItem.Hostname);
                        }

                        pingItem.Status = PingStatus.Error;
                        pingItem.PingResetEvent.Set();
                        pingItem.IsActive = false;
                        return;
                    }
                }
            }

            pingItem.PingResetEvent.Set();
        }


        public void backgroundThread_PerformTcpProbe(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;
            var pingItem = e.Argument as PingItem;
            
            var hostAndPort = pingItem.Hostname.Split(':');
            string hostname = hostAndPort[0];
            int portnumber;
            bool isPortValid;
            bool isPortOpen = false;
            if (int.TryParse(hostAndPort[1], out portnumber) && portnumber >= 1 && portnumber <= 65535)
                isPortValid = true;
            else
                isPortValid = false;

            if (!isPortValid)
            {
                // Error.
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => pingItem.AddHistory("Invalid port number.")));

                e.Cancel = true;
                pingItem.PingResetEvent.Set();
                pingItem.Status = PingStatus.Error;
                pingItem.IsActive = false;
                return;
            }

            // Check whether a hostname or an IP address was provided.  If hostname, resolve and print IP.
            var hostnameType = Uri.CheckHostName(hostname);
            if (hostnameType != UriHostNameType.IPv4 && hostnameType != UriHostNameType.IPv6)
            {
                var host = System.Net.Dns.GetHostEntry(hostname);
                if (host.AddressList.Length > 0)
                    Application.Current.Dispatcher.BeginInvoke(
                                new Action(() => pingItem.AddHistory("*** [" + host.AddressList[0].ToString() + "]")));
            }

            pingItem.Statistics = new PingStatistics();
            int errorCode = 0;

            while (!backgroundWorker.CancellationPending && pingItem.IsActive)
            {
                using (TcpClient client = new TcpClient())
                {
                    ++pingItem.Statistics.PingsSent;
                    DisplayStatistics(pingItem);

                    try
                    {
                        var result = client.BeginConnect(hostname, portnumber, null, null);
                        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                        if (!success)
                        {
                            throw new SocketException();
                        }

                        client.EndConnect(result);

                        if (backgroundWorker.CancellationPending || pingItem.IsActive == false)
                        {
                            pingItem.PingResetEvent.Set();
                            return;
                        }

                        // Check for status change.
                        if (pingItem.Status == PingStatus.Down)
                        {
                            backgroundWorker.ReportProgress(
                                0,
                                new StatusChangeLog { Timestamp = DateTime.Now, Hostname = pingItem.Hostname, Status = PingStatus.Up });
                            if (ApplicationOptions.EmailAlert)
                                SendEmail("up", pingItem.Hostname);
                        }

                        pingItem.DownCount = 0;
                        ++pingItem.Statistics.PingsReceived;
                        pingItem.Status = PingStatus.Up;
                        isPortOpen = true;
                    }
                    catch (SocketException ex)
                    {
                        const int WSAHOST_NOT_FOUND = 11001;

                        if (backgroundWorker.CancellationPending || pingItem.IsActive == false)
                        {
                            pingItem.PingResetEvent.Set();
                            return;
                        }

                        if (pingItem.Status == PingStatus.Up)
                            pingItem.Status = PingStatus.Indeterminate;
                        if (pingItem.Status == PingStatus.Inactive)
                            pingItem.Status = PingStatus.Down;
                        ++pingItem.DownCount;

                        // Check for status change.
                        if (pingItem.Status == PingStatus.Indeterminate && pingItem.DownCount >= ApplicationOptions.AlertThreshold)
                        {
                            pingItem.Status = PingStatus.Down;
                            backgroundWorker.ReportProgress(
                                0,
                                new StatusChangeLog { Timestamp = DateTime.Now, Hostname = pingItem.Hostname, Status = PingStatus.Down });
                            if (ApplicationOptions.EmailAlert)
                                SendEmail("down", pingItem.Hostname);
                        }

                        // If hostname cannot be resolved, report error and stop.
                        if (ex.ErrorCode == WSAHOST_NOT_FOUND)
                        {
                            e.Cancel = true;
                            Application.Current.Dispatcher.BeginInvoke(
                                new Action(() => pingItem.AddHistory("Unable to resolve hostname.")));

                            pingItem.Status = PingStatus.Error;
                            pingItem.PingResetEvent.Set();
                            pingItem.IsActive = false;
                            return;
                        }

                        ++pingItem.Statistics.PingsLost;
                        isPortOpen = false;
                        errorCode = ex.ErrorCode;
                    }
                    client.Close();
                }
                DisplayTcpReply(pingItem, isPortOpen, portnumber, errorCode);
                DisplayStatistics(pingItem);
                pingItem.PingResetEvent.Set();

                Thread.Sleep(ApplicationOptions.PingInterval < 4000 ? 4000 : ApplicationOptions.PingInterval);
            }

            pingItem.PingResetEvent.Set();
        }


        public void DisplayIcmpReply(PingItem pingItem)
        {
            if (pingItem.Reply == null)
                return;
            if (pingItem.PingBackgroundWorker.CancellationPending)
                return;

            var pingOutput = new StringBuilder($"[{DateTime.Now.ToLongTimeString()}]  ");

            // Read the status code of the ping response.
            switch (pingItem.Reply.Status)
            {
                case IPStatus.Success:
                    pingOutput.Append("Reply from ");
                    pingOutput.Append(pingItem.Reply.Address.ToString());
                    if (pingItem.Reply.RoundtripTime < 1)
                        pingOutput.Append("  [<1ms]");
                    else
                        pingOutput.Append($"  [{pingItem.Reply.RoundtripTime} ms]");
                    break;
                case IPStatus.DestinationHostUnreachable:
                    pingOutput.Append("Reply  [Host unreachable]");
                    break;
                case IPStatus.DestinationNetworkUnreachable:
                    pingOutput.Append("Reply  [Network unreachable]");
                    break;
                case IPStatus.DestinationUnreachable:
                    pingOutput.Append("Reply  [Unreachable]");
                    break;
                case IPStatus.TimedOut:
                    pingOutput.Append("Request timed out.");
                    break;
                default:
                    pingOutput.Append(pingItem.Reply.Status.ToString());
                    break;
            }
            // Add response to the output window.
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => pingItem.AddHistory(pingOutput.ToString())));

            // If logging is enabled, write the response to a file.
            if (ApplicationOptions.LogOutput && ApplicationOptions.LogPath.Length > 0)
            {
                var logPath = $@"{ApplicationOptions.LogPath}\{pingItem.Hostname}.txt";
                using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(@logPath, true))
                {
                    outputFile.WriteLine(pingOutput.ToString().Insert(1, DateTime.Now.ToShortDateString() + " "));
                }
            }
        }


        public void DisplayTcpReply(PingItem pingItem, bool isPortOpen, int portnumber, int errorCode)
        {
            if (pingItem.PingBackgroundWorker.CancellationPending)
                return;

            // Prefix the ping reply output with a timestamp.
            var pingOutput = new StringBuilder($"[{DateTime.Now.ToLongTimeString()}]  Port {portnumber.ToString()}: ");
            if (isPortOpen)
                pingOutput.Append("OPEN");
            else
            {
                pingOutput.Append("CLOSED");
            }

            // Add response to the output window.
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => pingItem.AddHistory(pingOutput.ToString())));

            // If logging is enabled, write the response to a file.
            if (ApplicationOptions.LogOutput && ApplicationOptions.LogPath.Length > 0)
            {
                var index = pingItem.Hostname.IndexOf(':');
                var hostname = (index > 0) ? pingItem.Hostname.Substring(0, index) : pingItem.Hostname;
                var logPath = $@"{ApplicationOptions.LogPath}\{hostname}.txt";
                using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(@logPath, true))
                {
                    outputFile.WriteLine(pingOutput.ToString().Insert(1, DateTime.Now.ToShortDateString() + " "));
                }
            }
        }


        public void DisplayStatistics(PingItem pingItem)
        {
            // Update the ping statistics label with the current
            // number of pings sent, received, and lost.
            pingItem.PingStatisticsText =
                $"Sent: {pingItem.Statistics.PingsSent} Received: {pingItem.Statistics.PingsReceived} Lost: {pingItem.Statistics.PingsLost}";
        }


        private void txtOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            var textBoxAncestor = textBox.Parent;
            var svTextBox = textBoxAncestor as ScrollViewer;
            svTextBox.ScrollToBottom();
        }


        private void sliderColumns_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderColumns.Value > _pingItems.Count)
                sliderColumns.Value = _pingItems.Count;
        }


        private void tbHostname_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var pingTB = sender as TextBox;
                var pingItem = pingTB.DataContext as PingItem;
                PingStartStop(pingItem);

                int index = _pingItems.IndexOf(pingItem);
                if (index < _pingItems.Count - 1)
                {
                    var cp = icPingItems.ItemContainerGenerator.ContainerFromIndex(index + 1) as ContentPresenter;
                    var tb = (TextBox)cp.ContentTemplate.FindName("tbHostname", cp);

                    if (tb != null)
                        tb.Focus();
                }
            }
        }


        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (_pingItems.Count <= 1)
                return;

            var pingButton = sender as Button;
            var pingItem = pingButton.DataContext as PingItem;
            if (pingItem.PingBackgroundWorker != null)
                pingItem.PingBackgroundWorker.CancelAsync();
            _pingItems.Remove(pingItem);
            if (sliderColumns.Value > _pingItems.Count)
                sliderColumns.Value = _pingItems.Count;
            RefreshGlobalStartStop();
        }


        public void SendEmail(string hostStatus, string hostName)
        {
            var serverAddress = ApplicationOptions.EmailServer;
            var serverUser = ApplicationOptions.EmailUser;
            var serverPassword = ApplicationOptions.EmailPassword;
            var serverPort = ApplicationOptions.EmailPort;
            var mailFromAddress = ApplicationOptions.EmailFromAddress;
            var mailFromFriendly = "vmPing";
            var mailToAddress = ApplicationOptions.EmailRecipient;
            var mailSubject = $"[vmPing] {hostName} <> Host {hostStatus}";
            var mailBody =
                $"{hostName} is {hostStatus}.{Environment.NewLine}" +
                $"{DateTime.Now.ToLongDateString()}  {DateTime.Now.ToLongTimeString()}";

            var message = new MailMessage();

            try
            {
                var smtpClient = new SmtpClient();
                MailAddress fromAddress;
                if (mailFromFriendly.Length > 0)
                    fromAddress = new MailAddress(mailFromAddress, mailFromFriendly);
                else
                    fromAddress = new MailAddress(mailFromAddress);

                smtpClient.Host = serverAddress;

                if (serverUser.Length > 0 && serverPassword.Length > 0)
                {
                    NetworkCredential basicCredential =
                        new NetworkCredential(serverUser, serverPassword);
                    smtpClient.Credentials = basicCredential;
                }

                if (serverPort.Length > 0)
                    smtpClient.Port = Int32.Parse(serverPort);

                message.From = fromAddress;
                message.Subject = mailSubject;
                message.Body = mailBody;

                message.To.Add(mailToAddress);

                //Send the email.
                smtpClient.Send(message);
            }
            catch
            {
                // There was an error sending Email.
            }
            finally
            {
                message.Dispose();
            }
        }


        private void AlwaysOnTopExecute(object sender, ExecutedRoutedEventArgs e)
        {
            mnuOnTop.IsChecked = !mnuOnTop.IsChecked;
            ToggleAlwaysOnTop();
        }

        private void ProbeOptionsExecute(object sender, ExecutedRoutedEventArgs e)
        {
            DisplayProbeOptions();
        }

        private void LogOutputExecute(object sender, ExecutedRoutedEventArgs e)
        {
            DisplayLogOutput();
        }

        private void EmailAlertsExecute(object sender, ExecutedRoutedEventArgs e)
        {
            DisplayEmailAlerts();
        }


        private void StartStopExecute(object sender, ExecutedRoutedEventArgs e)
        {
            string toggleStatus = StartStopMenuHeader.Text;

            foreach (var pingItem in _pingItems)
            {
                if (toggleStatus == "_Stop All (F5)" && pingItem.IsActive)
                    PingStartStop(pingItem);
                else if (toggleStatus == "_Start All (F5)" && !pingItem.IsActive)
                    PingStartStop(pingItem);
            }
        }


        private void HelpExecute(object sender, ExecutedRoutedEventArgs e)
        {
            if (HelpWindow.openWindow != null)
                HelpWindow.openWindow.Activate();
            else
            {
                var helpWindow = new HelpWindow();
                helpWindow.Show();
            }
        }


        private void NewInstanceExecute(object sender, ExecutedRoutedEventArgs e)
        {
            var p = new System.Diagnostics.Process();
            p.StartInfo.FileName =
                System.Reflection.Assembly.GetExecutingAssembly().Location;
            try
            {
                p.Start();
            }

            catch
            {
                // do nothing.
            }
        }


        private void TraceRouteExecute(object sender, ExecutedRoutedEventArgs e)
        {
            var traceWindow = new TraceRouteWindow(ApplicationOptions.AlwaysOnTop);
            traceWindow.Show();
        }


        private void FloodHostExecute(object sender, ExecutedRoutedEventArgs e)
        {
            var floodWindow = new FloodHostWindow(ApplicationOptions.AlwaysOnTop);
            floodWindow.Show();
        }


        private void AddMonitorExecute(object sender, ExecutedRoutedEventArgs e)
        {
            _pingItems.Add(new PingItem());
        }


        private void mnuOnTop_Click(object sender, RoutedEventArgs e)
        {
            ToggleAlwaysOnTop();
        }

        private void mnuEmailAlerts_Click(object sender, RoutedEventArgs e)
        {
            DisplayEmailAlerts();
        }

        private void mnuLogOutput_Click(object sender, RoutedEventArgs e)
        {
            DisplayLogOutput();
        }

        private void mnuProbeOptions_Click(object sender, RoutedEventArgs e)
        {
            DisplayProbeOptions();
        }


        private void ToggleAlwaysOnTop()
        {
            ApplicationOptions.AlwaysOnTop = mnuOnTop.IsChecked;
            ApplicationOptions.SaveApplicationOption();

            foreach (Window window in Application.Current.Windows)
                window.Topmost = ApplicationOptions.AlwaysOnTop;
        }


        private void DisplayEmailAlerts()
        {
            if (ApplicationOptions.EmailAlert)
            {
                mnuEmailAlerts.IsChecked = false;
                ApplicationOptions.EmailAlert = false;
                ApplicationOptions.SaveApplicationOption();
                return;
            }

            // Display email alerts window
            ApplicationOptions.BlurWindows();
            var emailAlertWindow = new EmailAlertWindow();
            emailAlertWindow.Owner = this;

            emailAlertWindow.ShowDialog();
            mnuEmailAlerts.IsChecked = ApplicationOptions.EmailAlert;

            ApplicationOptions.RemoveBlurWindows();
        }


        private void DisplayLogOutput()
        {
            if (ApplicationOptions.LogOutput)
            {
                mnuLogOutput.IsChecked = false;
                ApplicationOptions.LogOutput = false;
                ApplicationOptions.SaveApplicationOption();
                return;
            }


            // Display folder browse dialog box.
            ApplicationOptions.BlurWindows();
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select a location for the log files.";
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                ApplicationOptions.LogPath = dialog.SelectedPath;
                ApplicationOptions.LogOutput = true;
            }
            else
            {
                ApplicationOptions.LogOutput = false;
            }
            mnuLogOutput.IsChecked = ApplicationOptions.LogOutput;
            ApplicationOptions.SaveApplicationOption();
            ApplicationOptions.RemoveBlurWindows();
        }

        private void DisplayProbeOptions()
        {
            if (OptionsWindow.openWindow != null)
                OptionsWindow.openWindow.Activate();
            else
            {
                var optionsWindow = new OptionsWindow();
                optionsWindow.Show();
            }
        }


        private void ClearAllPingItems()
        {
            foreach (var pingItem in _pingItems)
            {
                if (pingItem.PingBackgroundWorker != null)
                    pingItem.PingBackgroundWorker.CancelAsync();
            }
            _pingItems.Clear();
            RefreshGlobalStartStop();
        }

        private void RefreshFavorites()
        {
            var favoritesList = Favorite.GetFavoriteTitles();

            // Clear existing favorites menu.
            for (int i = mnuFavorites.Items.Count - 1; i > 2; --i)
                mnuFavorites.Items.RemoveAt(i);

            // Load favorites.
            foreach (var fav in favoritesList)
            {
                var menuItem = new MenuItem();
                menuItem.Header = fav;
                menuItem.Click += (s, r) =>
                {
                    ClearAllPingItems();

                    var selectedFavorite = s as MenuItem;
                    var favorite = Favorite.GetFavoriteEntry(selectedFavorite.Header.ToString());
                    if (favorite.Hosts.Count < 1)
                        AddHostMonitor(1);
                    else
                    {
                        AddHostMonitor(favorite.Hosts.Count);
                        for (int i = 0; i < favorite.Hosts.Count; ++i)
                        {
                            _pingItems[i].Hostname = favorite.Hosts[i].HostAddr.ToUpper();
                            _pingItems[i].FriendlyName = favorite.Hosts[i].FriendlyName.ToUpper();
                            PingStartStop(_pingItems[i]);
                        }
                    }

                    sliderColumns.Value = favorite.ColumnCount;
                };

                mnuFavorites.Items.Add(menuItem);
            }
        }

        private void RefreshApplicationsOptions()
        {
           ApplicationOptions.LoadApplicationOption();
            mnuEmailAlerts.IsChecked = ApplicationOptions.EmailAlert;
            mnuLogOutput.IsChecked = ApplicationOptions.LogOutput;
            mnuOnTop.IsChecked = ApplicationOptions.AlwaysOnTop;
            if (mnuOnTop.IsChecked)
                ToggleAlwaysOnTop();
            mnuPopupAlways.IsChecked = false;
            mnuPopupNever.IsChecked = false;
            mnuPopupWhenMinimized.IsChecked = false;
            switch (ApplicationOptions.PopupOption)
            {
                case ApplicationOptions.PopupNotificationOption.Always:
                    mnuPopupAlways.IsChecked = true;
                    break;
                case ApplicationOptions.PopupNotificationOption.Never:
                    mnuPopupNever.IsChecked = true;
                    break;
                case ApplicationOptions.PopupNotificationOption.WhenMinimized:
                    mnuPopupWhenMinimized.IsChecked = true;
                    break;
            }
        }


        private void mnuAddToFavorites_Click(object sender, RoutedEventArgs e)
        {
            // Display add to favorites window.
            ApplicationOptions.BlurWindows();
            var addToFavoritesWindow = new AddToFavoritesWindow();
            addToFavoritesWindow.Owner = this;
            if (addToFavoritesWindow.ShowDialog() == true)
            {
                var currentHostList = new FavoriteItem();
                currentHostList.Title = addToFavoritesWindow.FavoriteTitle;
                for (int i = 0; i < _pingItems.Count; ++i)
                    currentHostList.Hosts.Add(new FavoriteHostItem {FriendlyName = _pingItems[i].FriendlyName, HostAddr = _pingItems[i].Hostname});
                Favorite.AddFavoriteEntry(currentHostList, (int)sliderColumns.Value);
                RefreshFavorites();
            }

            ApplicationOptions.RemoveBlurWindows();
        }

        private void mnuManageFavorites_Click(object sender, RoutedEventArgs e)
        {
            // Display manage favorites window.
            ApplicationOptions.BlurWindows();
            var manageFavoritesWindow = new ManageFavoritesWindow();
            manageFavoritesWindow.Owner = this;
            manageFavoritesWindow.ShowDialog();
            RefreshFavorites();

            ApplicationOptions.RemoveBlurWindows();
        }

        private void mnuPopupNotification_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;

            mnuPopupAlways.IsChecked = false;
            mnuPopupNever.IsChecked = false;
            mnuPopupWhenMinimized.IsChecked = false;

            menuItem.IsChecked = true;

            switch (menuItem.Header.ToString())
            {
                case "Always":
                    ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.Always;
                    break;
                case "Never":
                    ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.Never;
                    break;
                case "When Minimized":
                    ApplicationOptions.PopupOption = ApplicationOptions.PopupNotificationOption.WhenMinimized;
                    break;
            }
            ApplicationOptions.SaveApplicationOption();
        }

        private void txtFriendlyName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (((TextBox)sender).Text != "FRIENDLY NAME")
                return;
            else
                ((TextBox)sender).Text = "";
        }

        private void txtFriendlyName_LostFocus(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(((TextBox)sender).Text))
                ((TextBox)sender).Text = "FRIENDLY NAME";
        }

        private void Grid_DblClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                var pingButton = sender as Grid;
                var pingItem = pingButton.DataContext as PingItem;
                DetailWindow view = new DetailWindow(pingItem);
                view.Show();
            }
        }
    }
}
