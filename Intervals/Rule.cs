using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Intervals
{
    public class Rule
    {
        private Window _win;
        private DialogWindow _dw;
        private Dictionary<string, System.Timers.Timer> _waitForReturnUser;

        public Rule()
        {
            this._win = new Window();
            this._win.Width = 300;
            this._win.Height = 400;
            this._win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this._win.ResizeMode = ResizeMode.NoResize;

            _dw = new DialogWindow();
            this._win.Content = _dw;
            this._win.Closing += (sender, args) =>
            {
                args.Cancel = true;
                this._win.Visibility = Visibility.Hidden;
            };

            this._waitForReturnUser = new Dictionary<string, System.Timers.Timer>();
        }

        public void Click()
        {
            if (this._win.Visibility == Visibility.Hidden)
            {
                this._win.Visibility = Visibility.Visible;
            }
            else if (this._win.Visibility == Visibility.Visible)
            {
                this._win.Activate();
            }
            else
            {
                this._win.Show();
            }
        }

        public string Name => "间隔回复";

        public Action<string, string> SendMsg
        {
            set;
            get;
        }

        public Tuple<string, string> Me { set; get; }

        public string MsgHandle(string userName, string msg, int type)
        {
            var isAutoCheck = this._win.Dispatcher.Invoke(() => { return this._dw.autoCheckBox.IsChecked; });

            if (isAutoCheck == true && (msg.Contains("@" + Me.Item1) || (type == 1 && !userName.StartsWith("@@"))))
            {
                var autoContent = this._win.Dispatcher.Invoke(() => { return this._dw.autoContent.Text; });
                var autoTime = this._win.Dispatcher.Invoke(() => { return int.Parse(this._dw.autoTime.Text); });

                if (_waitForReturnUser.ContainsKey(userName))
                {
                    _waitForReturnUser[userName].Stop();
                    _waitForReturnUser[userName].Close();
                    _waitForReturnUser.Remove(userName);
                }

                var timer = new System.Timers.Timer(autoTime * 1000);
                _waitForReturnUser.Add(userName, timer);

                Console.WriteLine("add: " + _waitForReturnUser.Count);
                timer.Elapsed += (a, b) =>
                {
                    timer.Stop();
                    if (!_waitForReturnUser.ContainsKey(userName)) return;

                    Console.WriteLine("timer: " + _waitForReturnUser.Count);
                    SendMsg(userName, autoContent);
                    _waitForReturnUser.Remove(userName);
                };

                timer.Start();
            }

            return null;
        }

        public string FromMe(string userName, string msg, int type)
        {
            if (_waitForReturnUser.ContainsKey(userName))
            {
                _waitForReturnUser[userName].Stop();
                _waitForReturnUser[userName].Close();
                _waitForReturnUser.Remove(userName);
            }


            Console.WriteLine("FromMe: " + _waitForReturnUser.Count);

            return null;
        }

        public void BingDing(string userName)
        {
            var isIntervalCheck = this._win.Dispatcher.Invoke(() => { return this._dw.intervalCheckBox.IsChecked; });

            if (isIntervalCheck == true)
            {
                var intervalTime = this._win.Dispatcher.Invoke(() => { return int.Parse(this._dw.intervalTime.Text); });
                var timer = new System.Timers.Timer(intervalTime * 1000);

                timer.Elapsed += (a, b) =>
                {
                    timer.Stop();
                    isIntervalCheck = this._win.Dispatcher.Invoke(() => { return this._dw.intervalCheckBox.IsChecked; });

                    if (isIntervalCheck != true) return;

                    var intervalContent = this._win.Dispatcher.Invoke(() => { return this._dw.intervalContent.Text; });
                    intervalTime = this._win.Dispatcher.Invoke(() => { return int.Parse(this._dw.intervalTime.Text); });
                    timer.Interval = intervalTime *1000;
                    SendMsg(userName, intervalContent);
                    timer.Start();
                };

                timer.Start();
            }
        }
    }
}
