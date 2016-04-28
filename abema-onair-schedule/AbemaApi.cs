using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace abema_onair_schedule {
    class AbemaApi {
        // jsから取得する
        public String SecretKey = "";
        // guid
        public String DeviceId = "";
        // /v1/users の戻り値
        public String UserId = "";
        // /v1/users の戻り値
        public long CreatedAt = 0;
        // /v1/users の戻り値
        public String AuthToken = "";
        public static AbemaApi instance = new AbemaApi();
        private AbemaApi() {
            ServicePointManager.Expect100Continue = false;
        }
        public void loadToken() {
            // 既存のファイルがあれば読み込む
            if (loadSettingFile() && checkToken()) {
                return;
            }
            // 無かったので取得しなおし
            Console.WriteLine("トークンファイルを再取得します");
            String secretKey = getSecretKey();
            String deviceId, userId, authToken;
            long createdAt;
            {
                var r = getUserTokens(secretKey);
                deviceId = r.Item1;
                userId = r.Item2;
                createdAt = r.Item3;
                authToken = r.Item4;
            }
            {
                // 保存
                var hash = new Dictionary<String, Object>();
                hash["SecretKey"] = secretKey;
                hash["DeviceId"] = deviceId;
                hash["UserId"] = userId;
                hash["CreatedAt"] = createdAt;
                hash["AuthToken"] = authToken;
                String saveJson = JsonConvert.SerializeObject(hash, Formatting.Indented);
                Console.WriteLine($"トークンを再取得しました\n{saveJson}");
                System.IO.File.WriteAllText(settingPath(), saveJson);
            }
            this.DeviceId = deviceId;
            this.UserId = userId;
            this.CreatedAt = createdAt;
            this.AuthToken = authToken;
        }
        String settingPath() {
            String iniPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            iniPath = System.IO.Path.ChangeExtension(iniPath, ".tokens.json");
            return iniPath;
        }
        Boolean loadSettingFile() {
            if (System.IO.File.Exists(settingPath()) == false) {
                Console.WriteLine($"トークンファイルがありません [{settingPath()}]");
                return false;
            }
            String raw = System.IO.File.ReadAllText(settingPath());
            var obj = JsonConvert.DeserializeObject<Dictionary<String, Object>>(raw);
            this.SecretKey = (String)obj["SecretKey"];
            this.DeviceId = (String)obj["DeviceId"];
            this.UserId = (String)obj["UserId"];
            this.CreatedAt = (long)obj["CreatedAt"];
            this.AuthToken = (String)obj["AuthToken"];
            return true;
        }
        Boolean checkToken() {
            Console.WriteLine("トークンの有効確認をします");
            using (System.Net.WebClient wc = new System.Net.WebClient()) {
                try {
                    wc.Headers[System.Net.HttpRequestHeader.Authorization] = $"bearer {this.AuthToken}";
                    wc.Encoding = System.Text.Encoding.UTF8;
                    String json = wc.DownloadString("https://api.abema.io/v1/media/token?osName=pc&osVersion=1.0.0&osLang=ja&osTimezone=Asia%2FTokyo");
                    var obj = JsonConvert.DeserializeObject<Dictionary<String, String>>(json);
                    if (10 < obj["token"].Length) {
                        Console.WriteLine("トークンは有効でした");
                        return true;
                    }
                } catch (Exception) { }
            }
            Console.WriteLine("トークンが無効化されていました");
            return false;
        }
        String getSecretKey() {
            Console.WriteLine("main.jsから秘密鍵を取得します");
            using (System.Net.WebClient wc = new System.Net.WebClient()) {
                try {
                    wc.Encoding = System.Text.Encoding.UTF8;
                    String js = wc.DownloadString("https://abema.tv/main.js");
                    var reg = new System.Text.RegularExpressions.Regex(@"c=r\(l\),f=""(.+?)""");
                    var mat = reg.Match(js);
                    if (mat.Success) {
                        Console.WriteLine($"秘密鍵を取得しました[{mat.Groups[1].Value.Substring(0, 10)}...]");
                        return mat.Groups[1].Value;
                    }
                } catch (Exception) { }
            }
            Console.WriteLine($"秘密鍵の取得に失敗しました");
            return "";
        }
        static String getKeySecret(String guid, DateTime time, String secretKey) {
            Func<byte[], String> ss = (input) => {
                return Convert.ToBase64String(input).Replace("=", "").Replace("+", "-").Replace("/", "_");
            };
            var hmacSha256 = new HMACSHA256(Encoding.ASCII.GetBytes(secretKey));
            var hash = hmacSha256.ComputeHash(Encoding.ASCII.GetBytes(secretKey));
            time = time.ToUniversalTime();
            for (var i = 0; i < time.Month; i++) {
                hash = hmacSha256.ComputeHash(hash);
            }
            hash = hmacSha256.ComputeHash(Encoding.ASCII.GetBytes(ss(hash) + guid));
            for (var i = 0; i < time.Day % 5; i++) {
                hash = hmacSha256.ComputeHash(hash);
            }
            hash = hmacSha256.ComputeHash(Encoding.ASCII.GetBytes(ss(hash) + new DateTimeOffset(time).ToUnixTimeSeconds()));
            for (var i = 0; i < time.Hour % 5; i++) {
                hash = hmacSha256.ComputeHash(hash);
            }
            return ss(hash);
        }
        /// <summary>
        /// 番組表を取得する
        /// </summary>
        /// <param name="bearerHeader">authorizationヘッダーの値</param>
        /// <returns></returns>
        static Tuple<String, String, long, String> getUserTokens(String secretKey) {
            String deviceId = "";
            String userId = "";
            long createdAt = 0;
            String token = "";
            using (System.Net.WebClient wc = new System.Net.WebClient()) {
                try {
                    var time = DateTime.Now;
                    time = time.AddMinutes(-time.Minute).AddSeconds(-time.Second).AddHours(1);
                    var guid = Guid.NewGuid().ToString();
                    wc.Encoding = Encoding.ASCII;
                    String uploadText = $@"{{""applicationKeySecret"":""{getKeySecret(guid, time, secretKey)}"",""deviceId"":""{guid}""}}";
                    wc.Credentials = new NetworkCredential("abema", "goto");
                    wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    var res = wc.UploadString("https://api.abema.io/v1/users", uploadText);
                    var data = Newtonsoft.Json.Linq.JObject.Parse(res);
                    deviceId = guid;
                    userId = data["profile"]["userId"].ToString();
                    createdAt = int.Parse(data["profile"]["createdAt"].ToString());
                    token = data["token"].ToString();
                    return new Tuple<string, string, long, string>(deviceId, userId, createdAt, token);
                } catch (WebException ex) {
                    Console.WriteLine(ex.ToString());
                }
            }
            return null;
        }

    }
}
