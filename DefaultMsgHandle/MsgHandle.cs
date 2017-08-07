using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WXLogin;
using Newtonsoft.Json.Linq;

namespace DefaultMsgHandle
{
    public class MsgHandle : IWXMsgHandle
    {
        private readonly WXService _wxService;

        public MsgHandle()
        {
            _wxService = WXService.Instance;
        }

        public string Handle(JToken msgInfo)
        {
            var msg = msgInfo["Content"].ToString();
            var msgType = msgInfo["MsgType"].ToString();
            var appMsgType = msgInfo["AppMsgType"].ToString();
            var subMsgType = msgInfo["SubMsgType"].ToString();
            var fromUserName = msgInfo["FromUserName"].ToString();

            if (fromUserName.StartsWith("@@"))
            {
                msg = Regex.Replace(msg, @"^(@[a-zA-Z0-9]+|[a-zA-Z0-9_-]+):<br/>", string.Empty);
            }

            // text msg
            if (msgType == "1")
            {
                if (subMsgType == "48")
                    return $"对方给你发来位置信息: {msg.Split(new[] { ":<br/>" }, StringSplitOptions.RemoveEmptyEntries)[0]}";
                if (fromUserName.StartsWith("@@")) // 群消息
                    return WXService.DecodeMsgFace(msg);
                return fromUserName.Equals("newsapp") ? "[腾讯新闻消息]" : WXService.DecodeMsgFace(msg.Replace("<br/>", string.Empty));
            }

            // maybe from brand contact
            if (msgType == "49")
            {
                if (appMsgType == "5")
                {
                    // brandContact msg
                    var msged = _wxService.AllContactCache.Single(o => o.UserName == fromUserName).UserType != UserType.BrandContact ?
                        "[链接]: " : "来自公众号的消息: ";
                    var el = System.Xml.Linq.XElement.Parse(WXService.HtmlDecode(msg).Replace("<br/>", string.Empty));
                    var allItems = el.Element("appmsg")?.Element("mmreader")?.Element("category")?.Elements("item");
                    if (allItems == null)
                    {
                        msged += "\r\nTitle: " + WXService.DecodeMsgFace(el.Element("appmsg")?.Element("title")?.Value);
                        msged += "\r\nDigest: " + WXService.DecodeMsgFace(el.Element("appmsg")?.Element("des")?.Value);
                        //msged += "\r\nUrl: " + DecodeMsgFace(el.Element("appmsg")?.Element("url")?.Value);

                        return msged;
                    }

                    foreach (var item in allItems)
                    {
                        var title = WXService.DecodeMsgFace(item.Element("title")?.Value);
                        //var url = item.Element("url")?.Value;
                        var digest = WXService.DecodeMsgFace(item.Element("digest")?.Value);

                        msged += $"\r\nTitle: [{title}]\r\nDigest: [{digest}]"/*\r\nUrl: [{url}]*/;
                    }

                    return msged;
                }

                switch (appMsgType)
                {
                    case "2000":
                        return "[对方向你转账消息]";
                }
            }

            // other msg
            switch (msgType)
            {
                case "3":
                    return "[图片消息]";
                case "47":
                    return "[对方收藏图片]";
                case "34":
                    return "[语音消息]";
                case "43":
                    return "[视频]";
                case "62":
                    return "[小视频]";
                case "53":
                    return "[对方语音或视频呼叫你]";
                case "10000": // 包含红包消息
                    return msg;
                case "42":
                    return $"她向你推荐了名片: [{WXService.DecodeMsgFace(Regex.Match(msg, "nickname=\"(.*)\"").Groups[1].Value)}]";
                case "10002":
                    return "[对方撤回了一条消息]";
            }

            return msg;
        }
    }
}
