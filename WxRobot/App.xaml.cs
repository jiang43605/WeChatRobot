using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WXLogin;

namespace WxRobot
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                var main = new MainWindow(e.Args.FirstOrDefault());
                main.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                MessageBox.Show("严重错误，这会导致程序退出！");

                // loginout wechat
                var url = WXService._loginOut_url + LoginService.SKey;

                var sid = BaseService.GetCookie("wxsid");
                var uin = BaseService.GetCookie("wxuin");

                if (sid != null && uin != null)
                {
                    BaseService.SendPostRequest(url, $"sid={sid}&uin={uin}");
                }

                Environment.Exit(0);
            }
        }
    }
}
