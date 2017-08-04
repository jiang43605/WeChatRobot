using System;
using System.Collections.Generic;
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

namespace WxRobot
{
    public partial class MainWindow : Window
    {
        private WXService _wxSerivice;
        private IEnumerable<CheckBox> _allFriend;
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
                         this.codeImage.Source = ConvertByteToBitmapImage(ls.GetQRCode());
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
                             this.codeImage.Source = ConvertByteToBitmapImage(loginResult as byte[]);
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

            _wxSerivice.InitData(LastestCotactCacheUpdate);

            Dispatcher.InvokeAsync(() =>
            {
                this._allFriend = SetListBox(this.allList, _wxSerivice.AllContactCache);
            });
        }

        private void LastestCotactCacheUpdate(IEnumerable<WXUser> obj)
        {
            Dispatcher.InvokeAsync(() =>
            {
                SetListBox(this.recentList, obj);
            });
        }

        private async void InitPulginAsync()
        {
            await Task.Run(() =>
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugin\\");
                if (!Directory.Exists(path) || Directory.GetFiles(path).Length < 1) return;

                foreach (var item in Directory.GetFiles(path))
                {
                    if (System.IO.Path.GetExtension(item) != ".dll" && System.IO.Path.GetExtension(item) != ".exe") continue;

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
                var reMsg = default(string);
                var fromNickName = default(string);
                var msg = item.Msg;
                // update LatestContact
                // and will cache the user
                UpdateLatestContact(item);
                var nickNameInfo = _wxSerivice.GetNickName(item);
                if (nickNameInfo is string) fromNickName = (string)nickNameInfo;
                else
                {
                    var tp = nickNameInfo as Tuple<string, string, string>;
                    fromNickName = $"[{tp?.Item1}]{tp?.Item2}";
                    msg = tp?.Item3;
                }

                PrintLin($"[{fromNickName}] - [{_wxSerivice.Me.NickName}] - {DateTime.Now}");
                PrintLin($"Msg: {msg}");

                if (item.From.Equals(_wxSerivice.Me.UserName))
                {
                    // from me to other people
                    var toUser = Rule.Rules.Keys.FirstOrDefault(o => o.UserName == item.To);
                    if (toUser == null) return;

                    var toRule = Rule.Rules[toUser];
                    if (toRule.Name == "Default") continue;
                    reMsg = toRule.FromMeInvoke(toUser.UserName, msg, item.Type);
                }
                else
                {
                    // find the specify user rule, if aleady store in Rule.Rules
                    var user = Rule.Rules.Keys.FirstOrDefault(o => o.UserName == item.From);
                    if (user == null) continue;
                    var rule = Rule.Rules[user];

                    // if rule equals "Default", just ignore it
                    if (rule.Name == "Default") continue;
                    reMsg = rule.Invoke(user.UserName, msg, item.Type);
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

        private void UpdateLatestContact(WXMsg item)
        {
            if (_wxSerivice.LatestContactCache.Exists(o => o.UserName == item.From)) return;
            var newUser = _wxSerivice.AllContactCache.SingleOrDefault(o => o.UserName == item.From);
            if (newUser == null) return;

            _wxSerivice.AddItemToLatestContactCacheAsync(new[] { newUser });
        }

        /// <summary>
        /// set items to listBox, must work in UI thread
        /// </summary>
        /// <param name="listBox"></param>
        /// <param name="users"></param>
        private IEnumerable<CheckBox> SetListBox(ItemsControl listBox, IEnumerable<WXUser> users)
        {

            var listCheckBox = (listBox.ItemsSource as IEnumerable<CheckBox>)?.ToList() ?? new List<CheckBox>();

            foreach (var user in users)
            {
                var checkbox = new CheckBox
                {
                    MaxHeight = 20
                };

                var im = new Image
                {
                    Margin = new Thickness(0, 1, 0, 3)
                };

                var panel = new DockPanel();
                panel.Children.Add(im);
                panel.Children.Add(new Label
                {
                    Padding = new Thickness(0),
                    Content = user.NickName
                });

                checkbox.Content = panel;
                checkbox.DataContext = user;

                UpdateImageAsync(user, im);
                listCheckBox.Add(checkbox);

            }

            listBox.ItemsSource = listCheckBox.AsEnumerable();
            return listCheckBox;
        }

        private async void UpdateImageAsync(WXUser user, Image im)
        {
            await Task.Run(() =>
            {
                var icon = ConvertByteToBitmapImage(user.Icon);

                this.Dispatcher.InvokeAsync(() =>
                {
                    im.Source = icon;
                });
            });
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
        private BitmapImage ConvertByteToBitmapImage(byte[] bytes)
        {
            try
            {
                using (var m = new MemoryStream(bytes))
                {
                    using (var m1 = new MemoryStream())
                    {
                        System.Drawing.Image.FromStream(m).Save(m1, System.Drawing.Imaging.ImageFormat.Png);
                        var bitImage = new BitmapImage();
                        bitImage.BeginInit();
                        bitImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitImage.StreamSource = m1;
                        bitImage.EndInit();
                        bitImage.Freeze();
                        return bitImage;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("convert fail in bytes to BitmapImage: " + ex.Message);
                return null;
            }
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            var checkedList = new List<CheckBox>();
            var rule = (Rule)null;
            var log = string.Empty;
            foreach (var item in this.recentList.Items)
            {
                var obj = item as CheckBox;
                if (obj.IsChecked == true)
                    checkedList.Add(obj);
            }

            foreach (var item in this.allList.Items)
            {
                var obj = item as CheckBox;
                if (obj.IsChecked == true
                    && checkedList.Count(o => (o.DataContext as WXUser).NickName == (obj.DataContext as WXUser).NickName) < 1)
                    checkedList.Add(obj);
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

            foreach (var item in checkedList)
            {
                var panel = item.Content as DockPanel;
                var lb = panel.Children[1] as Label;
                lb.Content = Regex.Replace((lb.Content as string), @"\[Rule: .+\]", string.Empty) + $"[Rule: {rule.Name}]";
                lb.Foreground = Brushes.Red;

                var wxUser = item.DataContext as WXUser;
                if (Rule.SetBingDing(wxUser, rule)) log += $"Bingding {rule.Name} rule into {wxUser.NickName}\r\n";
                else log += $"Fail Bingding {rule.Name} rule into {wxUser.NickName}\r\n";
            }

            PrintLin(log);
        }

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //var el = sender as Label;
            //var saveList = new List<string>();
            //el.IsEnabled = false;

            //foreach (var item in this.recentList.Items)
            //{
            //    var cb = item as CheckBox;
            //    var dp = cb.Content as DockPanel;
            //    var lb = dp.Children[1] as Label;
            //    if (lb.Foreground == Brushes.Red) saveList.Add(lb.Content as string);
            //}

            //SetListBox(this.recentList, _wxSerivice.GetLatestContact());

            //Task.Run(() =>
            //{
            //    var originList = saveList
            //    .Select(o => Regex.Replace(o, @"\[Rule: .+\]", string.Empty))
            //    .ToList();

            //    this.recentList.Dispatcher.BeginInvoke(new Action(() =>
            //    {
            //        el.IsEnabled = true;

            //        foreach (var item in this.recentList.Items)
            //        {
            //            var cb = item as CheckBox;
            //            var dp = cb.Content as DockPanel;
            //            var lb = dp.Children[1] as Label;

            //            for (int i = 0; i < originList.Count; i++)
            //            {
            //                if (originList[i] != (lb.Content as string)) continue;
            //                lb.Foreground = Brushes.Red;
            //                lb.Content = saveList[i];
            //            }
            //        }
            //    }));
            //});
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
                this.allList.ItemsSource = this._allFriend;
            }

            this.allList.ItemsSource = _allFriend.Where(o => o.Content
            .Cast<DockPanel>().Children[1]
            .Cast<Label>().Content
            .Cast<string>().Contains(control.Text));
        }

        private void allSelect_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.allList.ItemsSource)
            {
                item.Cast<CheckBox>().IsChecked = true;
            }
        }

        private void allSelect_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.allList.ItemsSource)
            {
                item.Cast<CheckBox>().IsChecked = false;
            }
        }

        private void selectOther_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.allList.ItemsSource)
            {
                var status = item.Cast<CheckBox>().IsChecked;

                if (status == null) status = false;
                item.Cast<CheckBox>().IsChecked = !status;
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
