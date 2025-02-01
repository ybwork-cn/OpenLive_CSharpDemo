using Newtonsoft.Json;
using OpenBLive.Client;
using OpenBLive.Client.Data;
using OpenBLive.Runtime;
using OpenBLive.Runtime.Data;
using OpenBLive.Runtime.Utilities;
using System.Text;

namespace OpenBLiveSample
{
    internal class Program
    {
        //初始化于测试的参数
        public const string AccessKeyId = "Sdv1wrqU2Am3DI4xI1CwzWXo";//填入你的accessKeyId，可以在直播创作者服务中心【个人资料】页面获取(https://open-live.bilibili.com/open-manage)
        public const string AccessKeySecret = "8Y96R1a5waycBQl0GgWhOESWcOJhLM";//填入你的accessKeySecret，可以在直播创作者服务中心【个人资料】页面获取(https://open-live.bilibili.com/open-manage)
        public const string AppId = "1689295902472";//填入你的appId，可以在直播创作者服务中心【我的项目】页面创建应用后获取(https://open-live.bilibili.com/open-manage)
        public const string Code = "BSGFCJBOZMDC4";//填入你的主播身份码Code，可以在互动玩法首页，右下角【身份码】处获取(互玩首页：https://play-live.bilibili.com/)

        public static IBApiClient bApiClient = new BApiClient();
        public static string game_id = string.Empty;
        public static bool IsLive = false;

        public static async Task Main()
        {
            //是否为测试环境（一般用户可无视，给专业对接测试使用）
            BApi.isTestEnv = false;

            SignUtility.SetAccessKey(AccessKeyId, AccessKeySecret);
            var appId = AppId;
            var code = Code;

            Console.WriteLine("请输入自动关闭时间,不输入默认30秒");
            var closeTimeStr = Console.ReadLine();
            if (string.IsNullOrEmpty(closeTimeStr))
            {
                closeTimeStr = "30";
            }

            if (!string.IsNullOrEmpty(appId))
            {
                AppStartInfo startInfo = await bApiClient.StartInteractivePlay(code, appId);
                if (startInfo.Code != 0)
                {
                    Console.WriteLine(startInfo.Message);
                    return;
                }

                string gameId = startInfo.Data.GameInfo.GameId;

                game_id = gameId;
                IsLive = true;
                Console.WriteLine("成功开启，开始心跳，场次ID: " + gameId);

                //心跳API（用于保持在线）
                InteractivePlayHeartBeat m_PlayHeartBeat = new InteractivePlayHeartBeat(gameId);
                m_PlayHeartBeat.HeartBeatError += PlayHeartBeat_HeartBeatError;
                m_PlayHeartBeat.HeartBeatSucceed += PlayHeartBeat_HeartBeatSucceed;
                m_PlayHeartBeat.Start();

                //长链接（用户持续接收服务器推送消息）
                using WebSocketBLiveClient m_WebSocketBLiveClient = new WebSocketBLiveClient(startInfo.GetWssLink(), startInfo.GetAuthBody());
                m_WebSocketBLiveClient.OnDanmaku += WebSocketBLiveClientOnDanmaku;//弹幕事件
                m_WebSocketBLiveClient.OnGift += WebSocketBLiveClientOnGift;//礼物事件
                m_WebSocketBLiveClient.OnGuardBuy += WebSocketBLiveClientOnGuardBuy;//大航海事件
                m_WebSocketBLiveClient.OnSuperChat += WebSocketBLiveClientOnSuperChat;//SC事件
                m_WebSocketBLiveClient.OnLike += WebSocketBLiveClient_OnLike;//点赞事件(点赞需要直播间开播才会触发推送)
                m_WebSocketBLiveClient.OnEnter += WebSocketBLiveClient_OnEnter;//观众进入房间事件
                m_WebSocketBLiveClient.OnLiveStart += WebSocketBLiveClient_OnLiveStart;//直播间开始直播事件
                m_WebSocketBLiveClient.OnLiveEnd += WebSocketBLiveClient_OnLiveEnd;//直播间停止直播事件
                //m_WebSocketBLiveClient.Connect();//正常连接
                m_WebSocketBLiveClient.Connect(TimeSpan.FromSeconds(30));//失败后30秒重连

                await Task.Run(async () =>
                {
                    var closeTime = int.Parse(closeTimeStr);
                    await Task.Delay(closeTime * 1000);
                    var ret = await bApiClient.EndInteractivePlay(appId, gameId);
                    IsLive = false;
                    Console.WriteLine("关闭玩法: " + JsonConvert.SerializeObject(ret));
                    return;
                });
            }
        }

        private static void WebSocketBLiveClient_OnLiveEnd(LiveEnd liveEnd)
        {
            StringBuilder sb = new StringBuilder($"直播间[{liveEnd.room_id}]直播结束，分区ID：【{liveEnd.area_id}】,标题为【{liveEnd.title}】");
            Logger.Log(sb.ToString());
        }

        private static void WebSocketBLiveClient_OnLiveStart(LiveStart liveStart)
        {
            StringBuilder sb = new StringBuilder($"直播间[{liveStart.room_id}]开始直播，分区ID：【{liveStart.area_id}】,标题为【{liveStart.title}】");
            Logger.Log(sb.ToString());
        }

        private static void WebSocketBLiveClient_OnEnter(Enter enter)
        {
            StringBuilder sb = new StringBuilder($"用户[{enter.uname}]进入房间");
            Logger.Log(sb.ToString());
        }

        private static void WebSocketBLiveClient_OnLike(Like like)
        {
            StringBuilder sb = new StringBuilder($"用户[{like.uname}]点赞了{like.unamelike_count}次");
            Logger.Log(sb.ToString());
        }

        private static void PlayHeartBeat_HeartBeatSucceed()
        {
            //Logger.Log("心跳成功");
        }

        private static void PlayHeartBeat_HeartBeatError(string json)
        {
            JsonConvert.DeserializeObject<EmptyInfo>(json);
            Logger.Log("心跳失败" + json);
        }

        private static void WebSocketBLiveClientOnSuperChat(SuperChat superChat)
        {
            StringBuilder sb = new StringBuilder($"用户[{superChat.userName}]发送了{superChat.rmb}元的醒目留言内容：{superChat.message}");
            Logger.Log(sb.ToString());
        }

        private static void WebSocketBLiveClientOnGuardBuy(Guard guard)
        {
            StringBuilder sb = new StringBuilder($"用户[{guard.userInfo.userName}]充值了{(guard.guardUnit == "月" ? (guard.guardNum + "个月") : guard.guardUnit.TrimStart('*'))}[{(guard.guardLevel == 1 ? "总督" : guard.guardLevel == 2 ? "提督" : "舰长")}]大航海");
            Logger.Log(sb.ToString());
        }

        private static void WebSocketBLiveClientOnGift(SendGift sendGift)
        {
            StringBuilder sb = new StringBuilder($"用户[{sendGift.userName}]赠送了{sendGift.giftNum}个[{sendGift.giftName}]");
            Logger.Log(sb.ToString());
        }

        private static void WebSocketBLiveClientOnDanmaku(Dm dm)
        {
            StringBuilder sb = new StringBuilder($"用户[{dm.userName}]发送弹幕:{dm.msg}");
            Logger.Log(sb.ToString());
        }
    }
}
