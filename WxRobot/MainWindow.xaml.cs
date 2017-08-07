using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WXLogin;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Data;

namespace WxRobot
{
    public partial class MainWindow : Window
    {
        private WXService _wxSerivice;
        private string _arg;
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(string arg) : this()
        {
            this._arg = arg;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
        }

        private async void Init()
        {
            if (_arg == "plugin")
            {
                this.loginInfo.Visibility = Visibility.Hidden;
                this.setPanel.Visibility = Visibility.Visible;

                InitPulginAsync();
            }
            else
            {
                // wechat login
                var status = await InitLoginAsync();

                if (!status)
                {
                    // login fail
                    MessageBox.Show("登录失败,请稍等再试!");
                    Console.WriteLine("login fail, try again in an hour!");
                    Console.WriteLine("the more info: you may be banned by the WeChat, try again in an hour, maybe normal");
                    return;
                }

                // add pulgin to control panel
                InitPulginAsync();

                // add user control to panel
                InitUserData();

                // handle the msg
                OpenListening();
            }


        }
        private async Task<bool> InitLoginAsync()
        {
            this.loginLable.Content = "get the wechat code...";

            await Task.Run(() =>
             {
                 // start login
                 var ls = new LoginService();

                 if (ls.LoginCheck() == null)
                 {
                     this.Dispatcher.BeginInvoke(new Action(() =>
                     {
                         this.codeImage.Source = InternalHelp.ConvertByteToBitmapImage(ls.GetQRCode());
                         this.loginLable.Content = "please scan the code";
                     }));
                 }

                 // if the user scan and click login button
                 while (true)
                 {
                     var loginResult = ls.LoginCheck();

                     if (loginResult is byte[])
                     {
                         this.Dispatcher.BeginInvoke(new Action(() =>
                         {
                             this.codeImage.Source = InternalHelp.ConvertByteToBitmapImage(loginResult as byte[]);
                             this.loginLable.Content = "please click login button in you phone!";
                         }));
                     }
                     else if (loginResult is string)
                     {
                         ls.GetSidUid(loginResult as string);
                         break;
                     }
                 }
             });

            _wxSerivice = WXService.Instance;
            return _wxSerivice != null;
        }

        private void OpenListening()
        {
            Task.Run(() =>
            {
                try
                {
                    // create msg handle assembly
                    var allFile = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)
                    .Where(o => Path.GetExtension(o) == ".dll");
                    foreach (var file in allFile)
                    {
                        var type = Assembly.LoadFrom(file).GetTypes()
                        .FirstOrDefault(o => o != typeof(IWXMsgHandle) && typeof(IWXMsgHandle).IsAssignableFrom(o));

                        if (type == null) continue;
                        _wxSerivice.MsgHandle = Activator.CreateInstance(type) as IWXMsgHandle;
                        break;
                    }

                    // begin Listen the msg
                    _wxSerivice.Listening(ListeningHandle);
                }
                catch (LoginOutException)
                {
                    MessageBox.Show("在其它地方登录！程序将退出！");
                    Environment.Exit(0);
                }
                catch (Exception e)
                {
                    Console.WriteLine("OpenListening: " + e.Message);
                }
            });
        }

        private void InitUserData()
        {
            this.loginInfo.Visibility = Visibility.Hidden;
            this.setPanel.Visibility = Visibility.Visible;
            this.recentList.ItemsSource = WXService.RecentContactList;
            this.allList.ItemsSource = WXService.AllContactList;

            WXService.RecentContactList.CollectionChanged += UpdateImage;
            WXService.AllContactList.CollectionChanged += UpdateImage;

            _wxSerivice.InitData();
        }

        private void UpdateImage(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (WXUserViewModel item in e.NewItems)
            {
                Task.Run(() =>
                {
                    var iconBytes = item.UserName.Contains("@@")
                        ? WXLogin.WXService.Instance.GetHeadImg(item.UserName)
                        : WXLogin.WXService.Instance.GetIcon(item.UserName);
                    item.BitMapImage = InternalHelp.ConvertByteToBitmapImage(iconBytes);
                });
            }
        }
        private async void InitPulginAsync()
        {
            await Task.Run(() =>
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugin\\");
                if (!Directory.Exists(path) || Directory.GetFiles(path).Length < 1) return;

                foreach (var item in Directory.GetFiles(path))
                {
                    if (Path.GetExtension(item) != ".dll" && Path.GetExtension(item) != ".exe") continue;

                    var tp = Assembly.LoadFrom(item).GetTypes().FirstOrDefault(o => o.Name == "Rule");
                    if (tp == null) continue;
                    var obj = (object)null;

                    Dispatcher.Invoke(() =>
                    {
                        obj = Activator.CreateInstance(tp);
                    });


                    // set SendMsg value
                    tp.GetProperty("SendMsg")?.SetValue(obj, new Action<string, string>((to, msg) =>
                    {
                        PrintLin($"[{_wxSerivice.Me.ShowName}] - [{_wxSerivice.GetNickName(to, msg)}] - {DateTime.Now}");
                        PrintLin($"Msg: {msg}\r\n");
                        _wxSerivice.SendMsgAsync(msg, _wxSerivice.Me.UserName, to, 1);
                    }));

                    if (_arg != "plugin")
                        tp.GetProperty("Me")?.SetValue(obj, new Tuple<string, string>(_wxSerivice.Me.NickName, _wxSerivice.Me.UserName));

                    SetRuleListBox(obj);
                }
            });
        }

        private void ListeningHandle(IEnumerable<WXMsg> msgs)
        {
            foreach (var item in msgs)
            {
                string reMsg;
                var msg = item.Msg;

                PrintLin($"[{item.FromNickName}] - [{item.ToNickName}] - {DateTime.Now}");
                PrintLin($"Msg: {msg}");

                if (item.From.Equals(_wxSerivice.Me.UserName))
                {
                    // from me to other people
                    var toUser = Rule.Rules.Keys.FirstOrDefault(o => o.UserName == item.To);
                    if (toUser == null)
                    {
                        PrintLin(); continue;
                    }

                    var toRule = Rule.Rules[toUser];
                    if (toRule.Name == "Default")
                    {
                        PrintLin(); continue;
                    }
                    reMsg = toRule.FromMeInvoke(toUser.UserName, msg, item.Type, (int)toUser.UserType);
                }
                else
                {
                    // find the specify user rule, if aleady store in Rule.Rules
                    var user = Rule.Rules.Keys.FirstOrDefault(o => o.UserName == item.From);
                    if (user == null)
                    {
                        PrintLin(); continue;
                    };
                    var rule = Rule.Rules[user];

                    // if rule equals "Default", just ignore it
                    if (rule.Name == "Default")
                    {
                        PrintLin(); continue;
                    }
                    reMsg = rule.Invoke(user.UserName, msg, item.Type, (int)user.UserType);
                }

                PrintLin($"ReMsg: {reMsg}");
                PrintLin();

                // if reMsg is null, skip it
                if (reMsg != null)
                {
                    _wxSerivice.SendMsg(reMsg, _wxSerivice.Me.UserName, item.From, 1);
                }
            }
        }
        private void SetRuleListBox(object exObj)
        {
            this.Dispatcher.BeginInvoke(new Action<object>((obj) =>
            {
                var radio = new RadioButton();
                radio.GroupName = "rule";
                radio.DataContext = obj;

                ContentControl lb;
                var labelEvent = obj.GetType().GetMethod("Click");
                if (labelEvent != null)
                {
                    lb = new Button();
                    lb.PreviewMouseLeftButtonUp += (a, b) =>
                    {
                        labelEvent.Invoke(obj, null);
                    };
                }
                else
                {
                    lb = new Label();
                    lb.Style = this.Resources["linkLable"] as Style;
                }
                lb.Padding = new Thickness(0);
                lb.Content = obj.GetType().GetProperty("Name")?.GetValue(obj);
                lb.DataContext = labelEvent;

                radio.Content = lb;
                this.radios.Children.Add(radio);
            }), exObj);
        }

        /// <summary>
        /// print some log in textBox control
        /// </summary>
        private void Print(string msg)
        {
            this.print.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.print.Text += msg;
                this.print.ScrollToEnd();
            }));
        }
        private void PrintLin(string msg = "")
        {
            Print(msg + "\r\n");
        }


        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            var rule = (Rule)null;
            var log = string.Empty;

            var recentCheck = WXService.RecentContactList
                  .Where(o => o.IsCheck == true);
            var allCheck = WXService.AllContactList
                .Where(o => o.IsCheck == true);

            var finallyCheck = recentCheck.ToList();
            foreach (var item in allCheck)
            {
                if (finallyCheck.Contains(item, new CheckComparer()))
                {
                    item.IsCheck = false;
                    item.FontColor = "#FF000000";
                    continue;
                }

                finallyCheck.Add(item);
            }

            foreach (var item in this.radios.Children)
            {
                var obj = item as RadioButton;
                if (obj.IsChecked == false) continue;

                if (obj.DataContext != null)
                    rule = new Rule(obj.DataContext);
                else rule = Rule.Default;
                break;
            }

            foreach (var item in finallyCheck)
            {
                var originDisplayName = Regex.Replace(item.DisplayName, @"\[Rule: .+\]", string.Empty);

                var wxUser = _wxSerivice.AllContactCache.SingleOrDefault(o => o.UserName == item.UserName);
                if (Rule.SetBingDing(wxUser, rule)) log += $"Bingding {rule.Name} rule into {wxUser?.ShowName}\r\n";
                else
                {
                    rule = Rule.Default;
                    log += $"Fail Bingding {rule.Name} rule into {item.DisplayName} \r\n";
                }

                if (rule.Name.Equals(Rule.Default.Name))
                {
                    item.DisplayName = originDisplayName;
                    item.FontColor = "#FF000000";
                }
                else
                {
                    item.DisplayName = originDisplayName + $"[Rule: {rule.Name}]";
                    item.FontColor = "#FFFF0000";
                }
            }

            PrintLin(log);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_arg != "plugin" && _wxSerivice != null)
                WXService.LoginOut();
            Environment.Exit(0);
        }

        private void searchTb_KeyUp(object sender, KeyEventArgs e)
        {
            var control = sender.Cast<TextBox>();
            if (control.Text == string.Empty)
            {
                this.allList.ItemsSource = WXService.AllContactList;
            }

            this.allList.ItemsSource = WXService.AllContactList.Where(o => o.DisplayName.Contains(control.Text));
        }

        private void allSelect_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in WXService.AllContactList)
            {
                item.IsCheck = true;
            }
        }

        private void allSelect_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in WXService.AllContactList)
            {
                item.IsCheck = false;
            }
        }

        private void selectOther_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in WXService.AllContactList)
            {
                item.IsCheck = !(item.IsCheck ?? false);
            }
        }
    }

    public static class Extention
    {
        public static T Cast<T>(this object obj) where T : class
        {
            return obj as T;
        }
    }
}
