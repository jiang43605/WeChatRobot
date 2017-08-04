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
using System.Text.RegularExpressions;

namespace WXLogin
{
    /// <summary>
    /// 微信主要业务逻辑服务类
    /// </summary>
    public class WXService
    {
        private static WXService _wxService;
        private static Dictionary<string, string> _syncKey = new Dictionary<string, string>();
        private JObject _initResult;
        private WXUser _me;
        private List<WXUser> _latestContactCache;

        public event Action<IEnumerable<WXUser>> LastestContanctCacheUpdate;

        //微信初始化url
        private static string _init_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxinit?r=" + BaseService.GetTime(10);
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

        private const string _batch_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxbatchgetcontact?type=ex&r={0}&pass_ticket={1}";
        // login out
        public const string _loginOut_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxlogout?redirect=1&type=0&skey=";

        public static WXService Instance
        {
            get
            {
                if (_wxService == null)
                {
                    try
                    {
                        _wxService = new WXService();
                    }
                    catch (Exception e)
                    {
                        WXService.LoginOut();
                        Console.WriteLine(e.Message);
                        return null;
                    }
                }

                return _wxService;
            }
        }

        public WXUser Me
        {
            set { this._me = value; }
            get { return this._me; }
        }

        public string GetDeviceid
        {
            get
            {
                var r = new Random();
                var flag = r.NextDouble().ToString();

                while (flag.Length < 17)
                {
                    flag = r.NextDouble().ToString();
                }

                return "e" + flag.Substring(2, 15);
            }
        }

        /// <summary>
        /// 添加元素到最近联系人
        /// </summary>
        /// <param name="users"></param>
        public async void AddItemToLatestContactCacheAsync(IEnumerable<WXUser> users)
        {
            await Task.Run(() =>
            {
                if (this._latestContactCache == null)
                    this._latestContactCache = new List<WXUser>();

                lock (_latestContactCache) { this._latestContactCache.AddRange(users); }
                OnLastestContanctCacheUpdate(users);
            });
        }
        /// <summary>
        /// 获取好友完整列表
        /// </summary>
        public List<WXUser> AllContactCache { get; private set; }
        /// <summary>
        /// 获取最近好友列表,这是一个copy对象
        /// </summary>
        public List<WXUser> LatestContactCache
        {
            get
            {
                if (this._latestContactCache == null)
                    this._latestContactCache = new List<WXUser>();
                var list = new WXUser[_latestContactCache.Count];
                _latestContactCache.CopyTo(list);
                return list.ToList();
            }
        }

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
            Console.WriteLine("begin init wx construct");
            string init_json = "{{\"BaseRequest\":{{\"Uin\":\"{0}\",\"Sid\":\"{1}\",\"Skey\":\"{2}\",\"DeviceID\":\"" + GetDeviceid + "\"}}}}";
            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                init_json = string.Format(init_json, uin.Value, sid.Value, LoginService.SKey.Replace("@", "%40"));
                byte[] bytes = BaseService.SendPostRequest(_init_url + "&pass_ticket=" + LoginService.Pass_Ticket + "&lang=zh_CN", init_json);

                if (bytes == null)
                {
                    return false;
                }

                string init_str = Encoding.UTF8.GetString(bytes);
                JObject init_result = JsonConvert.DeserializeObject(init_str) as JObject;

                foreach (JObject synckey in init_result["SyncKey"]["List"])  //同步键值
                {
                    _syncKey.Add(synckey["Key"].ToString(), synckey["Val"].ToString());
                }

                // init fail
                if (_syncKey.Count == 0) return false;
                Console.WriteLine("get _syncKey");

                this._initResult = init_result;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 初始化联系人数据
        /// </summary>
        public void InitData(Action<IEnumerable<WXUser>> action = null)
        {
            if (action != null) this.LastestContanctCacheUpdate += action;

            Console.WriteLine("begin init InitLatestContact()");
            InitLatestContact();

            Console.WriteLine("begin init InitContact()");
            InitContact();
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
        public void InitContact()
        {

            var bytes = BaseService.SendGetRequest(_getcontact_url);
            var contact_str = Encoding.UTF8.GetString(bytes);

            var contact_result = JsonConvert.DeserializeObject(contact_str) as JObject;
            var contact_all = new List<WXUser>();

            if (contact_result == null)
            {
                AllContactCache = contact_all;
                return;
            }

            contact_all.AddRange(from JObject contact in contact_result["MemberList"]
                                 select new WXUser
                                 {
                                     UserType = UserType.Friend,
                                     UserName = contact["UserName"].ToString(),
                                     City = contact["City"].ToString(),
                                     HeadImgUrl = contact["HeadImgUrl"].ToString(),
                                     NickName = contact["NickName"].ToString(),
                                     Province = contact["Province"].ToString(),
                                     PYQuanPin = contact["PYQuanPin"].ToString(),
                                     RemarkName = contact["RemarkName"].ToString(),
                                     DisplayName = contact["DisplayName"].ToString(),
                                     RemarkPYQuanPin = contact["RemarkPYQuanPin"].ToString(),
                                     Sex = contact["Sex"].ToString(),
                                     Signature = contact["Signature"].ToString()
                                 });

            AllContactCache = contact_all;
        }
        /// <summary>
        /// 获取所有好友列表,在初次初始化时候被调用
        /// </summary>
        /// <param name="msg"></param>
        public async void InitAllContactAsync(string msg)
        {
            await Task.Run(() =>
             {
                 var allList = msg.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                     .Where(o => o.StartsWith("@"))
                     .Where(o => AllContactCache.All(k => k.UserName != o));

                 if (allList.Count() == 0) return;
                 lock (AllContactCache) { AllContactCache.AddRange(GetBatchContact(allList)); }
             });
        }
        /// <summary>
        /// 获取最近好友列表
        /// </summary>
        public void InitLatestContact()
        {
            if (this._initResult == null) return;
            var list = (from JObject contact in _initResult["ContactList"]
                        select new WXUser
                        {
                            UserType = UserType.Unkown,
                            UserName = contact["UserName"].ToString(),
                            City = contact["City"].ToString(),
                            HeadImgUrl = contact["HeadImgUrl"].ToString(),
                            NickName = contact["NickName"].ToString(),
                            Province = contact["Province"].ToString(),
                            PYQuanPin = contact["PYQuanPin"].ToString(),
                            RemarkName = contact["RemarkName"].ToString(),
                            DisplayName = contact["DisplayName"].ToString(),
                            RemarkPYQuanPin = contact["RemarkPYQuanPin"].ToString(),
                            Sex = contact["Sex"].ToString(),
                            Signature = contact["Signature"].ToString()
                        }).ToList();

            Console.WriteLine("InitLatestContact");
            Console.WriteLine("get first laster friends");
            AddItemToLatestContactCacheAsync(list);

            Task.Run(() =>
            {
                // check other in ChatSet
                var chatset = _initResult["ChatSet"].Value<string>();
                var otherItems = chatset.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(o => o.StartsWith("@"))
                    .Where(o => _latestContactCache.All(k => k.UserName != o));

                Console.WriteLine("get other in chatset");
                AddItemToLatestContactCacheAsync(GetBatchContact(otherItems));
            });
        }
        /// <summary>
        /// get public friend and charoom
        /// </summary>
        /// <param name="usernames"></param>
        /// <returns></returns>
        private List<WXUser> GetBatchContact(IEnumerable<string> usernames)
        {
            var sid = BaseService.GetCookie("wxsid");
            var uin = BaseService.GetCookie("wxuin");
            if (sid == null || uin == null) return null;

            // build the parameter
            var url = string.Format(_batch_url, BaseService.GetTime(), LoginService.Pass_Ticket);
            var us = usernames as string[] ?? usernames.ToArray();
            var jsonObject = new
            {
                BaseRequest = new
                {
                    DeviceID = GetDeviceid,
                    Sid = sid.Value,
                    Skey = LoginService.SKey,
                    Uin = uin.Value
                },
                Count = us.Count(),
                List = us.Select(o => new
                {
                    EncryChatRoomId = string.Empty,
                    UserName = o
                })
            };
            var jsonString = JsonConvert.SerializeObject(jsonObject);

            // requst http
            var byteInfos = BaseService.SendPostRequest(url, jsonString);
            var jObject = JToken.Parse(Encoding.UTF8.GetString(byteInfos));

            Console.WriteLine($"ContactList:" + jObject["Count"]);
            return jObject["ContactList"].Select(o =>
             {
                 var userName = o["UserName"].Value<string>();
                 var remarkName = o["RemarkName"].Value<string>();
                 var nickName = o["NickName"].Value<string>();
                 var signature = o["Signature"].Value<string>();
                 var headImgUrl = o["HeadImgUrl"].Value<string>();
                 var pYQuanPin = o["PYQuanPin"].Value<string>();
                 var remarkPYQuanPin = o["RemarkPYQuanPin"].Value<string>();
                 if (userName.StartsWith("@@"))
                 {
                     return new WXUser
                     {
                         UserType = UserType.ChatRoom,
                         UserName = userName,
                         RemarkName = remarkName,
                         NickName = nickName,
                         Signature = signature,
                         HeadImgUrl = headImgUrl,
                         PYQuanPin = pYQuanPin,
                         RemarkPYQuanPin = remarkPYQuanPin,
                         MemberList = o["MemberList"].Select(k => new WXUser
                         {
                             DisplayName = k["DisplayName"].Value<string>(),
                             NickName = k["NickName"].Value<string>(),
                             PYQuanPin = k["PYQuanPin"].Value<string>(),
                             UserName = k["UserName"].Value<string>()
                         }).ToList()
                     };
                 }
                 if (userName.StartsWith("@"))
                 {
                     return new WXUser
                     {
                         UserType = UserType.Unkown,
                         UserName = userName,
                         NickName = nickName,
                         Signature = signature,
                         HeadImgUrl = headImgUrl,
                         PYQuanPin = pYQuanPin,
                         RemarkPYQuanPin = remarkPYQuanPin,
                     };
                 }

                 Console.WriteLine($"unkown useName: " + userName);
                 return null;
             }).ToList();
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
                var checkurl = string.Format(_synccheck_url, sid.Value, uin.Value, sync_key, (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds, LoginService.SKey.Replace("@", "%40"), GetDeviceid);

                var ckc = new CookieCollection();
                foreach (var item in BaseService.GetAllCookies(BaseService.CookiesContainer))
                {
                    var ck = new Cookie(item.Name, item.Value, "/", ".qq.com");
                    ckc.Add(ck);
                }

                byte[] bytes = BaseService.SendGetRequest(checkurl, ckc);
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
            string sync_json = "{{\"BaseRequest\" : {{\"DeviceID\":\"" + GetDeviceid + "\",\"Sid\":\"{1}\", \"Skey\":\"{5}\", \"Uin\":\"{0}\"}},\"SyncKey\" : {{\"Count\":{2},\"List\":[{3}]}},\"rr\" :{4}}}";
            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                string sync_keys = "";
                foreach (KeyValuePair<string, string> p in _syncKey)
                {
                    sync_keys += "{\"Key\":" + p.Key + ",\"Val\":" + p.Value + "},";
                }
                sync_keys = sync_keys.TrimEnd(',');
                sync_json = string.Format(sync_json, uin.Value, sid.Value, _syncKey.Count, sync_keys, (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds, LoginService.SKey);

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

            return null;
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
                "\"DeviceID\" : \"" + GetDeviceid + "\"," +
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

                /*byte[] bytes = */
                BaseService.SendPostRequest(_sendmsg_url + sid.Value + "&lang=zh_CN&pass_ticket=" + LoginService.Pass_Ticket, msg_json);
                //string send_result = Encoding.UTF8.GetString(bytes);
            }
        }

        public async void SendMsgAsync(string msg, string from, string to, int type)
        {
            await Task.Run(() =>
            {
                SendMsg(msg, from, to, type);
            });
        }
        /// <summary>
        /// 退出
        /// </summary>
        public static void LoginOut()
        {
            var url = _loginOut_url + LoginService.SKey.Replace("@", "%40");

            Cookie sid = BaseService.GetCookie("wxsid");
            Cookie uin = BaseService.GetCookie("wxuin");

            if (sid != null && uin != null)
            {
                if (BaseService.CookiesContainer == null) BaseService.CookiesContainer = new CookieContainer();

                var ckc = new CookieCollection();
                foreach (var item in BaseService.GetAllCookies(BaseService.CookiesContainer))
                {
                    var ck = new Cookie(item.Name, item.Value, "/", ".qq.com");
                    ckc.Add(ck);
                }

                BaseService.SendPostRequest(url, $"sid={sid.Value}&uin={uin.Value}", true, ckc);
            }
        }
        /// <summary>
        /// get nickName from userName
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public object GetNickName(WXMsg userName)
        {
            var un = this._latestContactCache.SingleOrDefault(o => o.UserName == userName.From)
                ?? this.AllContactCache.SingleOrDefault(o => o.UserName == userName.From);

            if (un == null) return "null";
            if (un.UserType != UserType.ChatRoom) return un.NickName;
            var splitString = userName.Msg.Split(new[] { ":<br/>" }, StringSplitOptions.RemoveEmptyEntries);
            return new Tuple<string, string, string>(un.NickName,
                    un.MemberList.Single(o => o.UserName == splitString[0]).NickName, splitString[1]);
        }

        /// <summary>
        /// 监听消息
        /// </summary>
        /// <param name="msgAction"></param>
        public void Listening(Action<IEnumerable<WXMsg>> msgAction)
        {
            while (true)
            {
                var numFlag = default(int);
                var sync_flag = WxSyncCheck();
                var selector = Regex.Match(sync_flag ?? string.Empty, "selector:\"\\s*?(\\d+)\\s*?\"").Groups[1].Value;
                var retcode = Regex.Match(sync_flag ?? string.Empty, "retcode:\"\\s*?(\\d+)\\s*?\"").Groups[1].Value;
                /*
                 * you can do more logic by such info
                 * retcode:
                 *  0 正常
                 *  1100 失败/退出微信
                 * selector:
                 *  0 正常
                 *  2 新的消息
                 *  7 进入/离开聊天界面
                 */

                if (retcode == "1101")
                {
                    WXService.LoginOut();
                    throw new LoginOutException("loginout");
                }

                if (selector == string.Empty || selector == "0")
                {
                    continue;
                }

                var sync_result = WxSync();
                if (sync_result == null) continue;
                if (sync_result["AddMsgCount"] == null || sync_result["AddMsgCount"].ToString() == "0") continue;

                var msgs = sync_result["AddMsgList"].Select(m => new WXMsg
                {
                    From = m["FromUserName"].ToString(),
                    Msg = m["Content"].ToString(),
                    Readed = false,
                    Time = DateTime.Now,
                    To = m["ToUserName"].ToString(),
                    Type = int.Parse(m["MsgType"].ToString())
                });

                numFlag = msgs.Count();
                foreach (var msg in sync_result["AddMsgList"])
                {
                    if (msg["MsgType"].Value<int>() != 51) continue;

                    Console.WriteLine("get a msg which the type=51");
                    InitAllContactAsync(msg["StatusNotifyUserName"].Value<string>());
                    numFlag--;
                }

                if (numFlag == 0) continue;
                Task.Run(() => { msgAction?.Invoke(msgs); });
            }
        }

        protected virtual void OnLastestContanctCacheUpdate(IEnumerable<WXUser> users)
        {
            Console.WriteLine("update recentListBox");
            LastestContanctCacheUpdate?.Invoke(users);
        }
    }


    [Serializable]
    public class LoginOutException : Exception
    {
        public LoginOutException() { }
        public LoginOutException(string message) : base(message) { }
        public LoginOutException(string message, Exception inner) : base(message, inner) { }
        protected LoginOutException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
