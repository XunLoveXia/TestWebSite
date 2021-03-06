﻿using CrowdFundingShop.Utility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;
using System.Web.SessionState;

namespace CrowdFundingShop.UI.Controllers.WAP
{
    public class OauthController : Controller
    {
        //protected override void OnAuthorization(AuthorizationContext filterContext)
        //{
        //    usercenter();
        //}
        //
        // GET: /Oauth/
        JavaScriptSerializer Jss = new JavaScriptSerializer();
        public static string urlParameters = "iscallback=1&";
        public static int CurrentPageType = 0;
        //用户中心                                                                                                                                                                                                                                                                                                                                                                                                                                                                         
        //public ActionResult usercenter()
        //{
        //    try
        //    {
        //        Session.Clear();
        //        if ((Session["user"] == null) && String.IsNullOrEmpty(Request.QueryString["unionid"]))
        //        {
        //            var AppID = ConfigurationManager.AppSettings["AppID"];
        //            var domainurl = ConfigurationManager.AppSettings["domainurl"];
        //            var red_url = Url.Encode(domainurl + "/oauth/usercenter");
        //            var oauth_url = ConfigurationManager.AppSettings["oauth_url"];
        //            return Redirect(oauth_url + "?backurl=" + red_url);
        //        }
        //        else
        //        {
        //            //具体逻辑
        //            return View("~/Views/GoodsList/List.cshtml");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }
        //}
        //微信用户授权                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
        public ActionResult userlogin(string backurl)
        {
            var AppID = ConfigurationManager.AppSettings["AppID"];
            var domainurl = ConfigurationManager.AppSettings["domainurl"];
            var red_url = Url.Encode(domainurl + "/Oauth/getuserwechatinfo");
            const string scope = "snsapi_userinfo";
            Session["backurl"] = backurl;
            var state = GetRandCode(8);
            Session[state] = state;
            return Redirect(GetCodeUrl(AppID, red_url, scope, state));
        }
        public ActionResult getuserwechatinfo(string state, string code)
        {
            try
            {
                var AppID = ConfigurationManager.AppSettings["AppID"];
                var AppSecret = ConfigurationManager.AppSettings["AppSecret"];
                string userString = "";
                userString = GetUserInfo(AppID, AppSecret, code);
                Session["user"] = userString;

                // 这里就直接封装成一个 saveUser 方法，来保存用户就可以了
                if (SaveUser(userString))
                {
                    string url = Session["backurl"].ToString();
                    return Redirect(url);
                }
                else
                {
                    BLL.BackgroundUserBll_log.AddLog("保存用户出错", "", "0.0.0.0");
                    return View("~/Views/GoodsList/List.cshtml?userinfo=保存用户出错");
                }

            }
            catch (Exception e)
            {
                BLL.BackgroundUserBll_log.AddLog("错误了", e.Message, "0.0.0.0");
                return View("~/Views/GoodsList/List.cshtml?userinfo=错误2");
            }
        }

        /// <summary>
        /// 对页面是否要用授权
        /// </summary>
        /// <param name="Appid"> 微信应用id </param>
        /// <param name="redirect_uri"> 回调页面</param>
        /// <param name="scope"> 应用授权作用域snsapi_base（不弹出授权页面，直接跳转，只能获取用户openid），snsapi_userinfo （弹出授权页面，可通过openid拿到昵称、性别、所在地。并且，即使在未关注的情况下，只要用户授权，也能获取其信息） </param>
        /// state is a random string
        /// <returns> 授权地址</returns>
        public string GetCodeUrl(string Appid, string redirect_uri, string scope, string state)
        {
            return string.Format("https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope={2}&state={3}&connect_redirect=1#wechat_redirect", Appid, redirect_uri, scope, state);
        }
        /// <summary>
        ///用code换取获取用户信息（包括非关注用户的）
        /// </summary>
        /// <param name="Appid"></param>
        /// <param name="Appsecret"></param>
        /// <param name="Code"> 回调页面带的code参数 </param>
        /// <returns> 获取用户信息（json格式） </returns>
        public string GetUserInfo(string Appid, string Appsecret, string Code)
        {
            try
            {
                var url = string.Format("https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_code", Appid, Appsecret, Code);
                var ReText = WebRequestPostOrGet(url, "");
                var DicText = (Dictionary<string, object>)Jss.DeserializeObject(ReText);
                if (!DicText.ContainsKey("openid"))
                {
                    //WriteTxt_Log("获取openid失败，错误码：" + DicText["errcode"].ToString());
                    BLL.BackgroundUserBll_log.AddLog("标记111", "获取openid失败，错误码：" + DicText["errcode"].ToString(), "0.0.0.0");
                    return "";
                }
                else
                {
                    //string token = GetExistAccessToken(DicText);
                    //BLL.BackgroundUserBll_log.AddLog("token测试", token, "0.0.0.0");
                    return WebRequestPostOrGet("https://api.weixin.qq.com/sns/userinfo?access_token=" + DicText["access_token"] + "&openid=" + DicText["openid"] + "&lang=zh_CN", "");
                }
            }
            catch (Exception ex)
            {
                BLL.BackgroundUserBll_log.AddLog("标记112", "ex：" + ex.Message, "0.0.0.0");
                //WriteTxt_Log("ex:" + ex.Message);
                return "";
                //throw;                                                                                                                                                                                                                                                                                                                                                                                                                                                  
            }
        }

        /// <summary>
        /// 判断access_token是否过期
        /// </summary>
        /// <returns></returns>
        public string GetExistAccessToken(Dictionary<string, object> DicText)
        {
            // 读取XML文件中的数据
            string filepath = Server.MapPath("/File/XMLToken.xml");
            StreamReader str = new StreamReader(filepath, System.Text.Encoding.UTF8);
            XmlDocument xml = new XmlDocument();
            xml.Load(str);
            str.Close();
            str.Dispose();
            string Token = xml.SelectSingleNode("xml").SelectSingleNode("AccessToken").InnerText;
            DateTime AccessExpires = Convert.ToDateTime(xml.SelectSingleNode("xml").SelectSingleNode("AccessExpires").InnerText);
            if (DateTime.Now >= AccessExpires)
            {
                xml.SelectSingleNode("xml").SelectSingleNode("AccessToken").InnerText = DicText["access_token"].ToString();
                DateTime _accessExpires = DateTime.Now.AddSeconds(Converter.TryToInt32((DicText["expires_in"])));
                xml.SelectSingleNode("xml").SelectSingleNode("AccessExpires").InnerText = _accessExpires.ToString();
                xml.Save(filepath);
                Token = DicText["access_token"].ToString();
            }
            return Token;
        }

        #region 生成随机字符
        /// <summary>
        /// 生成随机字符
        /// </summary>
        /// <param name="iLength"> 生成字符串的长度 </param>
        /// <returns> 返回随机字符串 </returns>
        public static string GetRandCode(int iLength)
        {
            string sCode = "";
            if (iLength == 0)
            {
                iLength = 4;
            }
            string codeSerial = "0,1,2,3,4,5,6,7,8,9,a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z";
            string[] arr = codeSerial.Split(',');
            int randValue = -1;
            Random rand = new Random(unchecked((int)DateTime.Now.Ticks));
            for (int i = 0; i < iLength; i++)
            {
                randValue = rand.Next(0, arr.Length - 1);
                sCode += arr[randValue];
            }
            return sCode;
        }
        #endregion

        #region Post/Get提交调用抓取
        /// <summary>
        /// Post/get 提交调用抓取
        /// </summary>
        /// <param name="url"> 提交地址</param>
        /// <param name="param"> 参数</param>
        /// <returns> string</returns>
        public static string WebRequestPostOrGet(string sUrl, string sParam)
        {
            try
            {
                Uri uriurl = new Uri(sUrl);
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(uriurl); //HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url + (url.IndexOf("?") > -1 ? "" : "?") + param);                                                                                                                                                                                                                                                                                                                                                                                                                                                           
                req.Method = "Post";
                req.Timeout = 120 * 1000;
                req.ContentType = "application/x-www-form-urlencoded;";
                if (!string.IsNullOrEmpty(sParam))
                {
                    byte[] bt = System.Text.Encoding.UTF8.GetBytes(sParam);
                    req.ContentLength = bt.Length;
                    using (Stream reqStream = req.GetRequestStream())//using 使用可以释放using段内的内存                                                                                                                                                                                                                                                                                                                                                                                                                                                           
                    {
                        reqStream.Write(bt, 0, bt.Length);
                        reqStream.Flush();
                    }
                }

                using (WebResponse res = req.GetResponse())
                {
                    //在这里对接收到的页面内容进行处理                                                                                                                                                                                                                                                                                                                                                                                                                                                    
                    Stream resStream = res.GetResponseStream();
                    StreamReader resStreamReader = new StreamReader(resStream, System.Text.Encoding.UTF8);
                    string resLine;
                    System.Text.StringBuilder resStringBuilder = new System.Text.StringBuilder();
                    while ((resLine = resStreamReader.ReadLine()) != null)
                    {
                        resStringBuilder.Append(resLine + System.Environment.NewLine);
                    }
                    resStream.Close();
                    resStreamReader.Close();
                    return resStringBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                //WriteTxt_Log(ex);
                BLL.BackgroundUserBll_log.AddLog("标记112", "错误内容：" + ex.Message, "0.0.0.0");
                return ex.Message;//url错误时候回报错
            }
        }
        #endregion

        #region 保存用户信息到服务器
        public bool SaveUser(string userString)
        {
            if (string.IsNullOrWhiteSpace(userString))
            {
                return false;
            }
            try
            {
                var data = (Dictionary<string, object>)Jss.DeserializeObject(userString);
                Model.ConsumerInfo consumerInfo = new Model.ConsumerInfo()
                {
                    WeiXinAccount = data["openid"].ToString(),
                    NickName = data["nickname"].ToString(),
                    Address = data["country"].ToString() + data["province"].ToString() + data["city"].ToString(),
                    HeadIcon = data["headimgurl"].ToString()
                };

                // 查询库中是否存在该用户信息
                Model.ConsumerInfo currentConsumer = BLL.ConsumerInfoBll.GetByWeiXinAccount(data["openid"].ToString());
                if (currentConsumer == null)
                {
                    // 第一次登录，保存用户信息，并返回ID
                    string consumerid = BLL.ConsumerInfoBll.Add(consumerInfo);
                    if (string.IsNullOrEmpty(consumerid))
                    {
                        return false;
                    }
                    consumerInfo.ID = Converter.TryToInt64(consumerid);
                }
                else
                {

                    consumerInfo.ID = currentConsumer.ID;
                }
                Identity.LoginConsumer = consumerInfo;
                return true;
            }
            catch (Exception e)
            {
                BLL.BackgroundUserBll_log.AddLog("标记119", "存用户时发生错误：" + e.Message + "；" + Session["user"].ToString(), "0.0.0.0");
                return false;
            }
        }
        #endregion

        #region 支付相关

        #endregion

    }
}
