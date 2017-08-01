using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tuling
{
    public class TulingChat
    {

        private Chengf.Cf_HttpWeb hw;

        public string GetChat(string key, string user, string chat)
        {
            try
            {
                hw.EncodingSet = "utf-8";
                var html = hw.PostOrGet($"http://www.tuling123.com/openapi/api?key={key}&userid={user}&info={chat}", Chengf.HttpMethod.GET).HtmlValue;
                return GetStringByCode(html).Replace("<br>", "").Replace("</br>", "");
            }
            catch (Exception)
            {
                return "图灵机器人发生未知错误，请检查设置";
            }
        }

        public TulingChat()
        {
            hw = new Chengf.Cf_HttpWeb();
        }

        /// <summary>
        /// 根据返回的code码返回相应的字符
        /// </summary>
        /// <returns></returns>
        private string GetStringByCode(string html)
        {
            JArray ja = new JArray(JsonConvert.DeserializeObject(html));
            string code = ja[0]["code"].ToString();
            switch (code)
            {
                case "100000": // 文本
                    return ja[0]["text"].ToString();
                case "305000": // 火车
                    return HuoCheText(ja);
                case "306000": // 航班
                    return HanBanText(ja);
                case "302000": // 新闻
                    return XinWenText(ja);
                case "308000": // 菜谱
                    return CaiPuText(ja);
                default:
                    return null;
            }
        }

        #region 以上类的两个帮助方法

        private readonly int num = 3; // 推送数目设置

        // 帮助分析多个的新闻
        private string XinWenText(JArray ja)
        {
            string rt = ja[0]["text"].ToString() + "\r\n";
            if (ja[0]["list"].ToList().Count > num)
            {
                for (int i = 0; i < num; i++)
                {
                    rt += string.Format("标题：{0}\r\n（来自：{1}）\r\n详情：{2}\r\n",
                            ja[0]["list"][i]["article"].ToString(),
                            ja[0]["list"][i]["source"].ToString(),
                            ja[0]["list"][i]["detailurl"].ToString()
                            );
                }
            }
            else
            {
                for (int i = 0; i < ja[0]["list"].ToList().Count; i++)
                {
                    rt += string.Format("标题：{0}\r\n（来自：{1}）\r\n详情：{2}\r\n",
                            ja[0]["list"][i]["article"].ToString(),
                            ja[0]["list"][i]["source"].ToString(),
                            ja[0]["list"][i]["detailurl"].ToString()
                            );
                }
            }
            return rt;
        }

        // 帮助分析多个的菜谱
        private string CaiPuText(JArray ja)
        {
            string rt = ja[0]["text"].ToString() + "\r\n";
            if (ja[0]["list"].ToList().Count > num)
            {
                for (int i = 0; i < num; i++)
                {
                    rt += string.Format("名称：{0}\r\n详情：{1}\r\n链接地址：{2}\r\n",
                            ja[0]["list"][i]["name"].ToString(),
                            ja[0]["list"][i]["info"].ToString(),
                            ja[0]["list"][i]["detailurl"].ToString()
                            );
                }
            }
            else
            {
                for (int i = 0; i < ja[0]["list"].ToList().Count; i++)
                {
                    rt += string.Format("名称：{0}\r\n详情：{1}\r\n链接地址：{2}\r\n",
                            ja[0]["list"][i]["name"].ToString(),
                            ja[0]["list"][i]["info"].ToString(),
                            ja[0]["list"][i]["detailurl"].ToString()
                            );
                }
            }
            return rt;
        }

        // 帮助分析多个航班
        private string HanBanText(JArray ja)
        {
            string rt = ja[0]["text"].ToString() + "\r\n";
            if (ja[0]["list"].ToList().Count > num)
            {
                for (int i = 0; i < num; i++)
                {
                    rt += string.Format("航班：{0}\r\n起飞时间：{1}，到达时间：{2}\r\n",
                            ja[0]["list"][i]["flight"].ToString(),
                            ja[0]["list"][i]["starttime"].ToString().Split(' ')[0],
                            ja[0]["list"][i]["starttime"].ToString().Split(' ')[1]
                            );
                }
            }
            else
            {
                for (int i = 0; i < ja[0]["list"].ToList().Count; i++)
                {
                    rt += string.Format("航班：{0}\r\n起飞时间：{1}，到达时间：{2}\r\n",
                            ja[0]["list"][i]["flight"].ToString(),
                            ja[0]["list"][i]["starttime"].ToString().Split(' ')[0],
                            ja[0]["list"][i]["starttime"].ToString().Split(' ')[1]
                            );
                }
            }

            return rt;
        }

        // 帮助分析多个火车
        private string HuoCheText(JArray ja)
        {
            string rt = ja[0]["text"].ToString() + "\r\n";
            if (ja[0]["list"].ToList().Count > num)
            {
                for (int i = 0; i < num; i++)
                {
                    rt += string.Format("{0}车次为：{1}\r\n起发站为：{2}，到达站为：{3}\r\n开车时间为：{4}到达时间为：{5}\r\n更多请访问：{6}\r\n",
                            "",
                            ja[0]["list"][i]["trainnum"].ToString(),
                            ja[0]["list"][i]["start"].ToString(),
                            ja[0]["list"][i]["terminal"].ToString(),
                            ja[0]["list"][i]["starttime"].ToString(),
                            ja[0]["list"][i]["endtime"].ToString(),
                            ja[0]["list"][i]["detailurl"].ToString()
                            );
                }
            }
            else
            {
                for (int i = 0; i < ja[0]["list"].ToList().Count; i++)
                {
                    rt += string.Format("{0}车次为：{1}\r\n起发站为：{2}，到达站为：{3}\r\n开车时间为：{4}到达时间为：{5}\r\n更多请访问：{6}\r\n",
                            "",
                            ja[0]["list"][i]["trainnum"].ToString(),
                            ja[0]["list"][i]["start"].ToString(),
                            ja[0]["list"][i]["terminal"].ToString(),
                            ja[0]["list"][i]["starttime"].ToString(),
                            ja[0]["list"][i]["endtime"].ToString(),
                            ja[0]["list"][i]["detailurl"].ToString()
                            );
                }
            }

            return rt;
        }
        #endregion
    }
}
