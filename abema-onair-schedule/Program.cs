using abema_onair_schedule.Output;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace abema_onair_schedule {
    class Program {
        static void Main(string[] args) {
            AbemaApi.instance.loadToken();
            var prop = new Properties();
            var mediaData = getMedia();
            if (mediaData == null) {
                Console.WriteLine("番組表の取得に失敗");
                return;
            }
            Console.WriteLine($"番組表を受信 {mediaData.channels.Count}局 {mediaData.channelSchedules.Aggregate(0, (a, b) => { a += b.slots.Count; return a; })}番組受信");
            var slots = new Dictionary<string, List<ScheduleDataset.Slot>>();
            foreach (var i in mediaData.channelSchedules) {
                if (slots.ContainsKey(i.channelId) == false) {
                    slots[i.channelId] = new List<ScheduleDataset.Slot>();
                }
                slots[i.channelId].AddRange(i.slots);
            }
            foreach (var i in slots.Values) {
                i.Sort((a, b) => {
                    return a.startAt > b.startAt ? 1 : -1;
                });
            }

            //1日、1局ごとに保存する。過去のデータはファイルが存在したら上書きしない
            if (prop.jsonLogDirectory != null) {
                foreach (var i in mediaData.channelSchedules) {
                    saveOneChannelAndDay(prop.jsonLogDirectory, i);
                }
            }
            String logText = DateTime.Now.ToString("yyyy-MM-dd(ddd)", CultureInfo.GetCultureInfo("en-us"));
            {
                // 受信したデータ全部
                var scheduleHtml = new ScheduleHtml();
                scheduleHtml.setChannels(mediaData.channels);
                scheduleHtml.addPrograms(mediaData.channelSchedules);
                if (prop.programAllDirectory != null) {
                    scheduleHtml.saveHtml(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.programAllDirectory, $@"program-all.html")), mediaData.channels);
                }
                if (prop.programAllLogDirectory != null) {
                    scheduleHtml.saveHtml(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.programAllLogDirectory, $@"program-all-{logText}.html")), mediaData.channels);
                }
                foreach (var i in mediaData.channels) {
                    if (prop.programChannelAllDirectory != null) {
                        scheduleHtml.saveHtml(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.programChannelAllDirectory, $@"program-all-{i.id}.html")), i);
                    }
                    if (prop.programChannelAllLogDirectory != null) {
                        scheduleHtml.saveHtml(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.programChannelAllLogDirectory, $@"program-all-{i.id}-{logText}.html")), i);
                    }
                }
            }
            {
                // 24時間前以降
                var scheduleHtml = new ScheduleHtml();
                scheduleHtml.setChannels(mediaData.channels);
                var endLimit = DateTime.Now - TimeSpan.FromHours(24);
                scheduleHtml.addPrograms(mediaData.channelSchedules, endLimit);
                if (prop.programLaterDirectory != null) {
                    scheduleHtml.saveHtml(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.programLaterDirectory, $@"program-later.html")), mediaData.channels);
                    // 特別にアニメ専門を作る
                    List<ScheduleDataset.Channel> animeChannels = mediaData.channels.FindAll(a => a.id.Contains("anime"));
                    scheduleHtml.saveHtml(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.programLaterDirectory, $@"program-later-anime.html")), animeChannels);
                }
                if (prop.programLaterLogDirectory != null) {
                    scheduleHtml.saveHtml(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.programLaterLogDirectory, $@"program-later-{logText}.html")), mediaData.channels);
                }
                foreach (var i in mediaData.channels) {
                    if (prop.programChannelLaterDirectory != null) {
                        scheduleHtml.saveHtml(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.programChannelLaterDirectory, $@"program-later-{i.id}.html")), i);
                    }
                    if (prop.programChannelLaterLogDirectory != null) {
                        scheduleHtml.saveHtml(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.programChannelLaterLogDirectory, $@"program-later-{i.id}-{logText}.html")), i);
                    }
                }
            }
            if (prop.allProgramCsvLogDirectory != null) {
                var apg = new AllProgramLog();
                List<String> markFlag = new List<string>();
                foreach (var i in mediaData.channelSchedules) {
                    foreach (var j in i.slots) {
                        apg.addProgram(j);
                        String a = JsonConvert.SerializeObject(j.flags) + JsonConvert.SerializeObject(j.mark);
                        if (markFlag.Contains(a) == false) {
                            markFlag.Add(a);
                        }
                    }
                }
                foreach (var i in mediaData.channels) {
                    apg.saveOneChannelLog(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.allProgramCsvLogDirectory, $@"allLog-{i.id}.txt")), i.id);
                }

            }
            if (prop.reprtProgramDirectory != null) {
                var apg = new ReportChannel();
                foreach (var i in mediaData.channelSchedules) {
                    foreach (var j in i.slots) {
                        apg.addProgram(j);
                    }
                }
                foreach (var i in mediaData.channels) {
                    apg.saveChannelLog(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.reprtProgramDirectory, $@"allLog-{i.id}.txt")), i.id);
                }
                String[] animeChannelIds = mediaData.channels.FindAll(a => a.id.Contains("anime")).Select<ScheduleDataset.Channel, String>(a => a.id).ToArray<String>();
                apg.saveChannelLog(System.IO.Path.GetFullPath(System.IO.Path.Combine(prop.reprtProgramDirectory, $@"allLog-アニメ全部.txt")), animeChannelIds);

            }
        }
        /// <summary>
        /// 日、局ごとに保存する。過去のデータはファイルが存在したら上書きしない
        /// </summary>
        /// <param name="cs"></param>
        static void saveOneChannelAndDay(String jsonLogDirectory, ScheduleDataset.ChannelSchedule cs) {
            // abema-news-20160101.json
            DateTime date = System.DateTime.ParseExact(cs.date, "yyyyMMdd", System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.None);
            DateTime today = new DateTime();
            today = today.AddHours(-1 * today.Hour);
            today = today.AddMinutes(-1 * today.Minute);
            today = today.AddSeconds(-1 * today.Second);
            today = today.AddMilliseconds(-1 * today.Millisecond);
            Boolean isBefore = false;
            if (date < today) {
                isBefore = true;
            }
            String saveFileName = $"{cs.channelId}-{cs.date}.json";
            String saveFilePath = System.IO.Path.GetFullPath(System.IO.Path.Combine(jsonLogDirectory, saveFileName));
            if (System.IO.File.Exists(saveFilePath) && isBefore) {
                return;
            }
            System.IO.File.WriteAllText(saveFilePath, JsonConvert.SerializeObject(cs, Formatting.Indented));
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
        static ScheduleDataset.ScheduleDataset getMedia() {
            ServicePointManager.Expect100Continue = false;
            using (System.Net.WebClient wc = new System.Net.WebClient()) {
                try {
                    DateTime dateFrom = DateTime.Now - TimeSpan.FromDays(7);
                    DateTime dateTo = DateTime.Now + TimeSpan.FromDays(7);
                    wc.Headers[System.Net.HttpRequestHeader.Authorization] = $"bearer {AbemaApi.instance.AuthToken}";
                    wc.Encoding = System.Text.Encoding.UTF8;
                    String json = wc.DownloadString($"https://api.abema.io/v1/media?dateFrom={dateFrom:yyyyMMdd}&dateTo={dateTo:yyyyMMdd}");
                    var data = JsonConvert.DeserializeObject<ScheduleDataset.ScheduleDataset>(json);
                    return data;
                } catch (WebException ex) {
                    Console.WriteLine(ex.ToString());
                }
            }
            return null;
        }
    }
}
