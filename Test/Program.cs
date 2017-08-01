using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WXLogin;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var ls = new LoginService();

            var codeStream = ls.GetQRCode();
            var fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\WX二维码.png", FileMode.Create);
            fs.Write(codeStream, 0, codeStream.Length);

            while (true)
            {
                var loginResult = ls.LoginCheck();

                if (loginResult is Stream)
                {
                    //已扫描 未登录
                    Console.WriteLine("please click login btton in you phone!");
                }
                else if (loginResult is string)
                {
                    //已完成登录
                    ls.GetSidUid(loginResult as string);
                    break;
                }
            }

            var wxService = new WXService();

            var allFriend = wxService.GetContact();

            wxService.Listening(msgs =>
            {
                foreach (var msg in msgs)
                {
                   // do you logic
                }
            });
        }
    }
}
