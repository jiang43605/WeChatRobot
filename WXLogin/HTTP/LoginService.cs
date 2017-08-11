using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace WXLogin
{
    /// <summary>
    /// 微信登录服务类
    /// </summary>
    public class LoginService
    {
        public static string Pass_Ticket = "";
        public static string SKey = "";
        private static string _session_id = null;

        //获取会话ID的URL
        private static string _session_id_url = "https://login.weixin.qq.com/jslogin?appid=wx782c26e4c19acffb";
        //获取二维码的URL
        private static string _qrcode_url = "https://login.weixin.qq.com/qrcode/"; //后面增加会话id
        //判断二维码扫描情况   200表示扫描登录  201表示已扫描未登录  其它表示未扫描
        private static string _login_check_url = "https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?loginicon=true&uuid="; //后面增加会话id

        /// <summary>
        /// 获取登录二维码
        /// </summary>
        /// <returns></returns>
        public byte[] GetQRCode()
        {
            byte[] bytes = BaseService.SendGetRequest(_session_id_url);
            _session_id = Encoding.UTF8.GetString(bytes).Split(new string[] { "\"" }, StringSplitOptions.None)[1];
            return BaseService.SendGetRequest(_qrcode_url + _session_id);
        }
        /// <summary>
        /// 登录扫描检测
        /// </summary>
        /// <returns></returns>
        public object LoginCheck()
        {
            if (_session_id == null)
            {
                return null;
            }
            byte[] bytes = BaseService.SendGetRequest(_login_check_url + _session_id);
            string login_result = Encoding.UTF8.GetString(bytes);
            if (login_result.Contains("=201")) //已扫描 未登录
            {
                string base64_image = login_result.Split(new string[] { "\'" }, StringSplitOptions.None)[1].Split(',')[1];
                byte[] base64_image_bytes = Convert.FromBase64String(base64_image);
                //转成图片
                return base64_image_bytes;
            }
            else if (login_result.Contains("=200"))  //已扫描 已登录
            {
                string login_redirect_url = login_result.Split(new string[] { "\"" }, StringSplitOptions.None)[1];
                return login_redirect_url;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 获取sid uid   结果存放在cookies中
        /// </summary>
        public bool GetSidUid(string login_redirect)
        {
            byte[] bytes = BaseService.SendGetRequest(login_redirect + "&fun=new&version=v2&lang=zh_CN");
            string pass_ticket = Encoding.UTF8.GetString(bytes);
            Pass_Ticket = pass_ticket.Split(new string[] { "pass_ticket" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
            if (Pass_Ticket.Contains("当前登录环境异常")) return false;

            SKey = pass_ticket.Split(new string[] { "skey" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');

            BaseService.SetCookie("pgv_pvi", GetPgv());
            BaseService.SetCookie("pgv_si", GetPgv("s"));
            BaseService.SetCookie("MM_WX_NOTIFY_STATE", "1");
            BaseService.SetCookie("MM_WX_SOUND_STATE", "1");
            BaseService.SetCookie("last_wxuin", BaseService.GetCookie("wxuin").Value);
            BaseService.SetCookie("login_frequency", "1");

            var loadtime = BaseService.GetCookie("wxloadtime");
            if (loadtime != null)
            {
                BaseService.SetCookie("wxloadtime", loadtime.Value /*+ "_expired"*/);
            }
            else
            {
                var time = (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
                BaseService.SetCookie("wxloadtime", time.ToString().Substring(0, 10) /*+ "_expired"*/);
            }

            return true;
        }

        private string GetPgv(string str = "")
        {
            var r = new Random();
            var num = r.NextDouble();
            if (num == 0) num = 0.5;

            var time = (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
            var result = Math.Round(2147483647 * num) * +time % 1E10;

            return str + result.ToString();
        }
    }
}
