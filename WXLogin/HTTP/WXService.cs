using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace WXLogin
{
    /// <summary>
    /// 微信主要业务逻辑服务类
    /// </summary>
    public class WXService
    {
        private static Dictionary<string, string> _syncKey = new Dictionary<string, string>();
        private JObject _initResult;
        private WXUser _me;
        private List<WXUser> _latestContactCache;
        private List<WXUser> _allContactCache;

        //微信初始化url
        private static string _init_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxinit?r=1377482058764";
        //获取好友头像
        private static string _geticon_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxgeticon?username=";
        //获取群聊（组）头像
        private static string _getheadimg_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxgetheadimg?username=";
        //获取好友列表
        private static string _getcontact_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxgetcontact";
        //同步检查url
        private static string _synccheck_url = "https://webpush.weixin.qq.com/cgi-bin/mmwebwx-bin/synccheck?sid={0}&uin={1}&synckey={2}&r={3}&skey={4}&deviceid={5}";
        //同步url
        private static string _sync_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxsync?sid=";
        //发送消息url
        private static string _sendmsg_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsg?sid=";
        // login out
        public const string _loginOut_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxlogout?redirect=1&type=0&skey=";

        public WXUser Me
        {
            set { this._me = value; }
            get { return this._me; }
        }

        public List<WXUser> LatestContactCache
        {
            get
            {
                if (_latestContactCache == null) _latestContactCache = GetLatestContact();
                return _latestContactCache;
            }
            set => _latestContactCache = value;
        }
        public List<WXUser> AllContactCache { get => _allContactCache; set => _allContactCache = value; }

        public WXService()
        {
            if (!WxInit()) throw new Exception("init error!");
            else
            {
                _me = new WXUser();
                _me.UserName = _initResult["User"]["UserName"].ToString();
                _me.City = "";
                _me.HeadImgUrl = _initResult["User"]["HeadImgUrl"].ToString();
                _me.NickName = _initResult["User"]["NickName"].ToString();
                _me.Province = "";
                _me.PYQuanPin = _initResult["User"]["PYQuanPin"].ToString();
                _me.RemarkName = _initResult["User"]["RemarkName"].ToString();
                _me.RemarkPYQuanPin = _initResult["User"]["RemarkPYQuanPin"].ToString();
                _me.Sex = _initResult["User"]["Sex"].ToString();
                _me.Signature = _initResult["User"]["Signature"].ToString();
            }
        }
        /// <summary>
        /// 微信初始化
        /// </summary>
        /// <returns></returns>
        private bool WxInit()
        {
            string init_json = "{{\"BaseRequest\":{{\"Uin\":\"{0}\",\"Sid\":\"{1}\",\"Skey\":\"\",\"DeviceID\":\"e1615250492\"}}}}";
            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                init_json = string.Format(init_json, uin.Value, sid.Value);
                byte[] bytes = BaseService.SendPostRequest(_init_url + "&pass_ticket=" + LoginService.Pass_Ticket, init_json);
                string init_str = Encoding.UTF8.GetString(bytes);

                JObject init_result = JsonConvert.DeserializeObject(init_str) as JObject;

                foreach (JObject synckey in init_result["SyncKey"]["List"])  //同步键值
                {
                    if (!_syncKey.ContainsKey(synckey["Key"].ToString()))
                        _syncKey.Add(synckey["Key"].ToString(), synckey["Val"].ToString());
                }

                this._initResult = init_result;
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 获取好友头像
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public byte[] GetIcon(string username)
        {
            return BaseService.SendGetRequest(_geticon_url + username);
        }
        /// <summary>
        /// 获取微信讨论组头像
        /// </summary>
        /// <param name="usename"></param>
        /// <returns></returns>
        public byte[] GetHeadImg(string usename)
        {
            return BaseService.SendGetRequest(_getheadimg_url + usename);
        }
        /// <summary>
        /// 获取好友列表
        /// </summary>
        /// <returns></returns>
        public List<WXUser> GetContact()
        {
            byte[] bytes = BaseService.SendGetRequest(_getcontact_url);
            string contact_str = Encoding.UTF8.GetString(bytes);

            var contact_result = JsonConvert.DeserializeObject(contact_str) as JObject;
            var contact_all = new List<WXUser>();

            if (contact_result != null)
            {
                foreach (JObject contact in contact_result["MemberList"])  //完整好友名单
                {
                    WXUser user = new WXUser();
                    user.UserName = contact["UserName"].ToString();
                    user.City = contact["City"].ToString();
                    user.HeadImgUrl = contact["HeadImgUrl"].ToString();
                    user.NickName = contact["NickName"].ToString();
                    user.Province = contact["Province"].ToString();
                    user.PYQuanPin = contact["PYQuanPin"].ToString();
                    user.RemarkName = contact["RemarkName"].ToString();
                    user.RemarkPYQuanPin = contact["RemarkPYQuanPin"].ToString();
                    user.Sex = contact["Sex"].ToString();
                    user.Signature = contact["Signature"].ToString();

                    contact_all.Add(user);
                }
            }

            return AllContactCache = contact_all;
        }

        public List<WXUser> GetLatestContact()
        {
            var _contact_latest = new List<WXUser>();

            if (this._initResult != null)
            {
                foreach (JObject contact in _initResult["ContactList"])  //部分好友名单
                {
                    WXUser user = new WXUser();
                    user.UserName = contact["UserName"].ToString();
                    user.City = contact["City"].ToString();
                    user.HeadImgUrl = contact["HeadImgUrl"].ToString();
                    user.NickName = contact["NickName"].ToString();
                    user.Province = contact["Province"].ToString();
                    user.PYQuanPin = contact["PYQuanPin"].ToString();
                    user.RemarkName = contact["RemarkName"].ToString();
                    user.RemarkPYQuanPin = contact["RemarkPYQuanPin"].ToString();
                    user.Sex = contact["Sex"].ToString();
                    user.Signature = contact["Signature"].ToString();

                    _contact_latest.Add(user);
                }
            }

            return LatestContactCache = _contact_latest;
        }
        /// <summary>
        /// 微信同步检测
        /// </summary>
        /// <returns></returns>
        private string WxSyncCheck()
        {
            string sync_key = "";
            foreach (KeyValuePair<string, string> p in _syncKey)
            {
                sync_key += p.Key + "_" + p.Value + "%7C";
            }
            sync_key = sync_key.TrimEnd('%', '7', 'C');

            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                _synccheck_url = string.Format(_synccheck_url, sid.Value, uin.Value, sync_key, (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds, LoginService.SKey.Replace("@", "%40"), "e1615250492");

                byte[] bytes = BaseService.SendGetRequest(_synccheck_url + "&_=" + DateTime.Now.Ticks);
                if (bytes != null)
                {
                    return Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 微信同步
        /// </summary>
        /// <returns></returns>
        private JObject WxSync()
        {
            string sync_json = "{{\"BaseRequest\" : {{\"DeviceID\":\"e1615250492\",\"Sid\":\"{1}\", \"Skey\":\"{5}\", \"Uin\":\"{0}\"}},\"SyncKey\" : {{\"Count\":{2},\"List\":[{3}]}},\"rr\" :{4}}}";
            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            string sync_keys = "";
            foreach (KeyValuePair<string, string> p in _syncKey)
            {
                sync_keys += "{\"Key\":" + p.Key + ",\"Val\":" + p.Value + "},";
            }
            sync_keys = sync_keys.TrimEnd(',');
            sync_json = string.Format(sync_json, uin.Value, sid.Value, _syncKey.Count, sync_keys, (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds, LoginService.SKey);

            if (sid != null && uin != null)
            {
                byte[] bytes = BaseService.SendPostRequest(_sync_url + sid.Value + "&lang=zh_CN&skey=" + LoginService.SKey + "&pass_ticket=" + LoginService.Pass_Ticket, sync_json);
                if (bytes == null) return null;
                string sync_str = Encoding.UTF8.GetString(bytes);

                JObject sync_resul = JsonConvert.DeserializeObject(sync_str) as JObject;

                if (sync_resul["SyncKey"]["Count"].ToString() != "0")
                {
                    _syncKey.Clear();
                    foreach (JObject key in sync_resul["SyncKey"]["List"])
                    {
                        _syncKey.Add(key["Key"].ToString(), key["Val"].ToString());
                    }
                }
                return sync_resul;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="type"></param>
        public void SendMsg(string msg, string from, string to, int type)
        {
            string msg_json = "{{" +
            "\"BaseRequest\":{{" +
                "\"DeviceID\" : \"e441551176\"," +
                "\"Sid\" : \"{0}\"," +
                "\"Skey\" : \"{6}\"," +
                "\"Uin\" : \"{1}\"" +
            "}}," +
            "\"Msg\" : {{" +
                "\"ClientMsgId\" : {8}," +
                "\"Content\" : \"{2}\"," +
                "\"FromUserName\" : \"{3}\"," +
                "\"LocalID\" : {9}," +
                "\"ToUserName\" : \"{4}\"," +
                "\"Type\" : {5}" +
            "}}," +
            "\"rr\" : {7}" +
            "}}";

            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                msg_json = string.Format(msg_json, sid.Value, uin.Value, msg, from, to, type, LoginService.SKey, DateTime.Now.Millisecond, DateTime.Now.Millisecond, DateTime.Now.Millisecond);

                byte[] bytes = BaseService.SendPostRequest(_sendmsg_url + sid.Value + "&lang=zh_CN&pass_ticket=" + LoginService.Pass_Ticket, msg_json);

                string send_result = Encoding.UTF8.GetString(bytes);
            }
        }

        public async void SendMsgAsync(string msg, string from, string to, int type)
        {
            await Task.Run(() =>
            {
                SendMsg(msg, from, to, type);
            });
        }
        public void LoginOut()
        {
            var url = _loginOut_url + LoginService.SKey;

            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                BaseService.SendPostRequest(url, $"sid={sid}&uin={uin}");
            }
        }
        /// <summary>
        /// get nickName from userName
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public string GetNickName(string userName)
        {
            if (this.LatestContactCache == null) GetLatestContact();
            var nickNameObject = this.LatestContactCache.SingleOrDefault(o => o.UserName == userName);

            if (this.AllContactCache == null) GetContact();
            if (nickNameObject == null) nickNameObject = this.AllContactCache.SingleOrDefault(o => o.UserName == userName);
            else return nickNameObject.NickName;

            try
            {
                if (nickNameObject == null) return GetLatestContact().Single(o => o.UserName == userName).NickName;
                else return nickNameObject.NickName;
            }
            catch
            {
                return userName;
            }

        }

        /// <summary>
        /// 监听消息
        /// </summary>
        /// <param name="msgAction"></param>
        public void Listening(Action<IEnumerable<WXMsg>> msgAction)
        {
            while (true)
            {
                var sync_flag = WxSyncCheck();
                if (sync_flag == null) continue;

                var sync_result = WxSync();
                if (sync_result == null) continue;
                if (sync_result["AddMsgCount"] != null && sync_result["AddMsgCount"].ToString() != "0")
                {
                    var msgs = sync_result["AddMsgList"].Select(m => new WXMsg
                    {
                        From = m["FromUserName"].ToString(),
                        Msg = m["Content"].ToString(),
                        Readed = false,
                        Time = DateTime.Now,
                        To = m["ToUserName"].ToString(),
                        Type = int.Parse(m["MsgType"].ToString())
                    }).Where(o => o.Type != 51);

                    Task.Run(() => { msgAction?.Invoke(msgs); });
                }
                else
                {
                    System.Threading.Thread.Sleep(500);
                }
            }
        }

    }
}
