﻿using Newtonsoft.Json;
using OpenBLive.Runtime.Data;
using OpenBLive.Runtime.Utilities;

namespace OpenBLive.Runtime
{
    /// <summary>
    /// 各类b站api
    /// </summary>
    public static class BApi
    {
        /// <summary>
        /// 是否为测试环境的api
        /// </summary>
        public static bool isTestEnv;

        /// <summary>
        /// 开放平台域名
        /// </summary>
        private static string OpenLiveDomain =>
            isTestEnv ? "http://test-live-open.biliapi.net" : "https://live-open.biliapi.com";

        /// <summary>
        /// 应用开启
        /// </summary>
        private const string k_InteractivePlayStart = "/v2/app/start";

        /// <summary>
        /// 应用关闭
        /// </summary>
        private const string k_InteractivePlayEnd = "/v2/app/end";

        /// <summary>
        /// 应用心跳
        /// </summary>
        private const string k_InteractivePlayHeartBeat = "/v2/app/heartbeat";

        /// <summary>
        /// 应用批量心跳
        /// </summary>
        private const string k_InteractivePlayBatchHeartBeat = "/v2/app/batchHeartbeat";


        private const string k_Post = "POST";



        public static async Task<string> StartInteractivePlay(string code, string appId)
        {
            var postUrl = OpenLiveDomain + k_InteractivePlayStart;
            var param = $"{{\"code\":\"{code}\",\"app_id\":{appId}}}";

            var result = await RequestWebUTF8(postUrl, k_Post, param);

            return result;
        }

        public static async Task<string> EndInteractivePlay(string appId, string gameId)
        {
            var postUrl = OpenLiveDomain + k_InteractivePlayEnd;
            var param = $"{{\"app_id\":{appId},\"game_id\":\"{gameId}\"}}";

            var result = await RequestWebUTF8(postUrl, k_Post, param);
            return result;
        }

        public static async Task<string> HeartBeatInteractivePlay(string gameId)
        {
            var postUrl = OpenLiveDomain + k_InteractivePlayHeartBeat;
            string param = "";
            if (gameId != null)
            {
                param = $"{{\"game_id\":\"{gameId}\"}}";

            }

            var result = await RequestWebUTF8(postUrl, k_Post, param);
            return result;
        }

        public static async Task<string> BatchHeartBeatInteractivePlay(string[] gameIds)
        {
            var postUrl = OpenLiveDomain + k_InteractivePlayBatchHeartBeat;
            GameIds games = new GameIds()
            {
                gameIds = gameIds
            };
            var param = JsonConvert.SerializeObject(games);
            var result = await RequestWebUTF8(postUrl, k_Post, param);
            return result;
        }

        private static async Task<string> RequestWebUTF8(string url, string method, string param,
            string cookie = null)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod(method), url);

            if (param != null)
            {
                SignUtility.SetReqHeader(requestMessage, param, cookie);
            }
            HttpResponseMessage res = await client.SendAsync(requestMessage);
            string result = await res.Content.ReadAsStringAsync();
            return result;
        }
    }
}
