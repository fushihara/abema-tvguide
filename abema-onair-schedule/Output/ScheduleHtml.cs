using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using abema_onair_schedule.ScheduleDataset;

namespace abema_onair_schedule.Output {
    class ScheduleHtml {
        class ChannelAndId {
            public String ChannelId = "";
            public String ChannelName = "";
        }
        static readonly int StartHour = 5;
        Dictionary<String, List<ScheduleDataset.Slot>> programsWithChId = new Dictionary<string, List<ScheduleDataset.Slot>>();
        List<ChannelAndId> allChannelIds = new List<ChannelAndId>();
        public void setChannels(List<Channel> channels) {
            channels.ForEach(a => this.allChannelIds.Add(new ChannelAndId { ChannelId = a.id, ChannelName = a.name }));
        }
        public void addPrograms(List<ScheduleDataset.ChannelSchedule> schedules, DateTime? checkDateBase = null) {
            schedules.Aggregate(new List<ScheduleDataset.Slot>(), (a, b) => {
                a.AddRange(b.slots);
                return a;
            })
            .Where(elem => { return checkDateBase == null ? true : checkDateBase <= elem.endAt ? true : false; })
            .ToList()
            .ForEach(elem => {
                String chId = elem.channelId;
                if (this.programsWithChId.ContainsKey(chId) == false) {
                    this.programsWithChId[chId] = new List<ScheduleDataset.Slot>();
                }
                this.programsWithChId[chId].Add(elem);
            });
        }
        public void saveHtml(String outPath, List<ScheduleDataset.Channel> saveChannels) {
            DateTime firstProgramTime = DateTime.Now, finalProgramTime = DateTime.Now;
            foreach (var i in this.programsWithChId.Values) {
                foreach (var j in i) {
                    firstProgramTime = j.startAt < firstProgramTime ? j.startAt : firstProgramTime;
                    finalProgramTime = finalProgramTime < j.endAt ? j.endAt : finalProgramTime;
                }
            }
            //0分0秒に合わせる
            firstProgramTime = firstProgramTime.AddMinutes(firstProgramTime.Minute * -1);
            firstProgramTime = firstProgramTime.AddSeconds(firstProgramTime.Second * -1);
            firstProgramTime = firstProgramTime.AddMilliseconds(firstProgramTime.Millisecond * -1);
            finalProgramTime = finalProgramTime.AddHours(1);
            finalProgramTime = finalProgramTime.AddMinutes(finalProgramTime.Minute * -1);
            finalProgramTime = finalProgramTime.AddSeconds(finalProgramTime.Second * -1);
            finalProgramTime = finalProgramTime.AddMilliseconds(finalProgramTime.Millisecond * -1);

            String val;
            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(myAssembly.GetManifestResourceStream(@"abema_onair_schedule.Resources.outHtml.txt"), System.Text.Encoding.UTF8)) {
                val = sr.ReadToEnd();

            }


            val = val.Replace("<!--ALL-CHANNELS-->", JsonConvert.SerializeObject(this.allChannelIds, Formatting.None));
            val = val.Replace("<!--THIS-FILE-CONTAIN-CHANNELS-->", JsonConvert.SerializeObject(saveChannels.Select(a => new ChannelAndId { ChannelId = a.id, ChannelName = a.name }), Formatting.None));
            val = val.Replace("<!--HEADERH-->", getHeaderHAllChannel(saveChannels));
            val = val.Replace("<!--HEADERV-->", getHeaderVAllTime(firstProgramTime, finalProgramTime));
            val = val.Replace("<!--MAIN-->", getProgramsPerStation(saveChannels, firstProgramTime, finalProgramTime));
            val = val.Replace("<!--CH-COUNT -->", saveChannels.Count.ToString());
            val = val.Replace("<!--FIRST-DATE-UNIX-SEC-->", new DateTimeOffset(firstProgramTime).ToUnixTimeSeconds().ToString());
            val = val.Replace("<!--FINAL-DATE-UNIX-SEC-->", new DateTimeOffset(finalProgramTime).ToUnixTimeSeconds().ToString());
            System.IO.File.WriteAllText(outPath, val);
        }

        public void saveHtml(String outPath, ScheduleDataset.Channel saveChannel) {
            DateTime firstProgramTime = DateTime.Now, finalProgramTime = DateTime.Now;
            foreach (var i in this.programsWithChId[saveChannel.id]) {
                firstProgramTime = i.startAt < firstProgramTime ? i.startAt : firstProgramTime;
                finalProgramTime = finalProgramTime < i.endAt ? i.endAt : finalProgramTime;
            }
            //0分0秒に合わせる
            firstProgramTime = firstProgramTime.AddMinutes(firstProgramTime.Minute * -1);
            firstProgramTime = firstProgramTime.AddSeconds(firstProgramTime.Second * -1);
            firstProgramTime = firstProgramTime.AddMilliseconds(firstProgramTime.Millisecond * -1);
            finalProgramTime = finalProgramTime.AddHours(1);
            finalProgramTime = finalProgramTime.AddMinutes(finalProgramTime.Minute * -1);
            finalProgramTime = finalProgramTime.AddSeconds(finalProgramTime.Second * -1);
            finalProgramTime = finalProgramTime.AddMilliseconds(finalProgramTime.Millisecond * -1);
            String val;
            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(myAssembly.GetManifestResourceStream("abema_onair_schedule.Resources.outHtml.txt"), System.Text.Encoding.UTF8)) {
                val = sr.ReadToEnd();
            }
            val = val.Replace("<!--ALL-CHANNELS-->", JsonConvert.SerializeObject(this.allChannelIds, Formatting.None));
            val = val.Replace("<!--THIS-FILE-CONTAIN-CHANNELS-->", JsonConvert.SerializeObject(new List<ChannelAndId> { new ChannelAndId { ChannelId = saveChannel.id, ChannelName = saveChannel.name } }, Formatting.None));
            val = val.Replace("<!--HEADERH-->", getHeaderHAllDay(firstProgramTime, finalProgramTime));
            val = val.Replace("<!--HEADERV-->", getHeaderVOneDay());
            val = val.Replace("<!--MAIN-->", getProgramsPerDay(saveChannel, firstProgramTime, finalProgramTime));
            val = val.Replace("<!--CH-COUNT -->", getDaysForPerChannelMode(firstProgramTime, finalProgramTime).Count.ToString());
            val = val.Replace("<!--FIRST-DATE-UNIX-SEC-->", new DateTimeOffset(new DateTime(2000, 1, 1, ScheduleHtml.StartHour, 0, 0)).ToUnixTimeSeconds().ToString());
            val = val.Replace("<!--FINAL-DATE-UNIX-SEC-->", new DateTimeOffset(new DateTime(2000, 1, 2, ScheduleHtml.StartHour, 0, 0)).ToUnixTimeSeconds().ToString());
            System.IO.File.WriteAllText(outPath, val);
        }
        static List<DateTime> getDaysForPerChannelMode(DateTime from, DateTime to) {
            List<DateTime> result = new List<DateTime>();
            Func<DateTime, DateTime> trimHour = (input) => {
                input = input.AddMinutes(input.Minute * -1);
                input = input.AddSeconds(input.Second * -1);
                input = input.AddMilliseconds(input.Millisecond * -1);
                return input;
            };
            from = trimHour(from);
            to = trimHour(to);

            from = (from.Hour < ScheduleHtml.StartHour ? from.AddDays(-1) : from).AddHours(-1 * from.Hour).AddHours(ScheduleHtml.StartHour);
            to = (to.Hour <= ScheduleHtml.StartHour ? to.AddDays(-1) : to).AddHours(-1 * to.Hour).AddHours(ScheduleHtml.StartHour);
            StringBuilder sb = new StringBuilder();
            for (var i = from; i <= to; i += TimeSpan.FromDays(1)) {
                result.Add(i);
            }
            return result;
        }
        // 横軸のhtmlを返す 引数のチャンネル一覧
        static String getHeaderHAllChannel(List<ScheduleDataset.Channel> channels) {
            // 局の一覧を出力
            StringBuilder sb = new StringBuilder();
            foreach (var i in channels) {
                sb.AppendLine($@"<th><a href=""https://abema.tv/now-on-air/{i.id}""><img style=""object-fit: contain;width: calc( var(--ch-width) - 3px);height: calc( var(--ch-height) - 1px );"" src=""https://hayabusa.io/abema/channels/logo/{i.id}.h130.webp""/></a></th>");
            }
            return "<table><tr>\n" + sb.ToString() + "\n</tr></table>";
        }
        // 横軸のhtmlを返す 日付の一覧
        static String getHeaderHAllDay(DateTime from, DateTime to) {
            StringBuilder sb = new StringBuilder();
            foreach (var i in getDaysForPerChannelMode(from, to)) {
                sb.AppendLine($"<th>{i:yyyy-MM-dd(ddd)}</th>");
            }
            return "<table><tr>\n" + sb.ToString() + "\n</tr></table>";
        }
        // 縦軸のhtmlを返す 引数で渡された全ての日時
        static String getHeaderVAllTime(DateTime from, DateTime to) {
            StringBuilder sb = new StringBuilder();
            for (var now = from; now < to; now += TimeSpan.FromHours(1)) {
                // <tr><th style="height:100px;">1</th></tr>
                String add = $@"<th style=""height: calc( var(--one-min-height) * 60 );""><div class=""ymd"">{now:yyyy}<br>{now:MM}/{now:dd}<br>({now:ddd})</div><div class=""hour"">{now:HH}</div></th>";
                sb.AppendLine($@"<tr>{add}</tr>");
            }
            return "<table>\n" + sb.ToString() + "\n</table>";
        }
        // 縦軸のhtmlを返す 24時間分のみ
        static String getHeaderVOneDay() {
            // 時間の一覧を出力
            StringBuilder sb = new StringBuilder();
            for (var i = ScheduleHtml.StartHour; i < ScheduleHtml.StartHour + 24; i++) {
                String backgroundColor = "";
                String textColor = "black";
                switch (i % 24) {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        backgroundColor = "linear-gradient(to bottom, #9370db 1%,#7757ba 100%)";
                        break;
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                        backgroundColor = "linear-gradient(to bottom, #20B2AA 1%,#109B94 100%)";
                        break;
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                    case 17:
                        backgroundColor = "linear-gradient(to bottom, #FFA07A 1%,#DE8460 100%)";
                        break;
                    case 18:
                    case 19:
                    case 20:
                    case 21:
                    case 22:
                    case 23:
                        backgroundColor = "linear-gradient(to bottom, #6495ED 1%,#4977C8 100%)";
                        break;
                    default:
                        break;
                }
                String add = $@"<th style=""height: calc( var(--one-min-height) * 60 );color:{textColor};background:{backgroundColor};""><div class=""hour"">{i % 24}</div></th>";
                sb.AppendLine($@"<tr>{add}</tr>");
            }
            return "<table>\n" + sb.ToString() + "\n</table>";
        }
        String getProgramsPerStation(List<ScheduleDataset.Channel> channels, DateTime from, DateTime to) {
            StringBuilder sb = new StringBuilder();
            foreach (var i in channels) {
                var slots = new List<ScheduleDataset.Slot>();
                double totalMinutes = (to - from).TotalMinutes;
                if (this.programsWithChId.ContainsKey(i.id)) {
                    foreach (var j in this.programsWithChId[i.id]) {
                        var add = new Tuple<ScheduleDataset.Slot, double, double>(j, (j.startAt - from).TotalMinutes, (j.endAt - j.startAt).TotalMinutes);
                        slots.Add(j);
                    }
                    slots.Sort((a, b) => {
                        return a.startAt < b.startAt ? -1 : 1;
                    });
                }
                sb.AppendLine($"<td>\n\n{getOneChannel(slots, from, to)}\n\n</td>\n");
            }
            return "<table><tr>\n" + sb.ToString() + "\n</tr></table>";
        }
        String getProgramsPerDay(ScheduleDataset.Channel channel, DateTime from, DateTime to) {
            StringBuilder sb = new StringBuilder();
            foreach (var i in getDaysForPerChannelMode(from, to)) {
                var slots = new List<ScheduleDataset.Slot>();
                double totalMinutes = 24 * 60;
                if (this.programsWithChId.ContainsKey(channel.id)) {


                    foreach (var j in this.programsWithChId[channel.id]) {
                        if (j.endAt <= i || (i + TimeSpan.FromHours(24)) <= j.startAt) {
                            continue;
                        }
                        double scrollOffset = Math.Max((j.startAt - i).TotalMinutes, 0);
                        double scrollHeight = (j.endAt - j.startAt).TotalMinutes;
                        scrollHeight = Math.Min(scrollHeight, totalMinutes - scrollOffset);
                        var add = new Tuple<ScheduleDataset.Slot, double, double>(j, scrollOffset, scrollHeight);
                        slots.Add(j);
                    }
                    slots.Sort((a, b) => {
                        return a.startAt < b.startAt ? -1 : 1;
                    });



                }
                sb.AppendLine($"<td>\n\n{getOneChannel(slots, i, i + TimeSpan.FromHours(24))}\n\n</td>\n");
            }
            return "<table><tr>\n" + sb.ToString() + "\n</tr></table>";
        }
        //static String getOneChannel(List<Tuple<ScheduleDataset.Slot, double, double>> programsWithOffsets, double listHeight) {
        static String getOneChannel(List<ScheduleDataset.Slot> slots, DateTime from, DateTime to) {
            double listHeight = (to - from).TotalMinutes;
            double beforeCellEndPoint = 0;
            StringBuilder sb = new StringBuilder();
            foreach (var slot in slots) {
                double slotPositionTop = (slot.startAt - from).TotalMinutes; ;
                double slotPositionBottom = (slot.endAt - from).TotalMinutes; ;
                double slotHeight = slot.Duration.TotalMinutes;

                if (beforeCellEndPoint < slotPositionTop) {
                    sb.Append($@"<tr><td class=""one-program-1""><div style=""overflow:hidden;height:calc( var(--one-min-height) * {(slotPositionTop - beforeCellEndPoint)} - 1px );""></div></td></tr>");
                    beforeCellEndPoint += (slotPositionTop - beforeCellEndPoint);
                }
                {
                    double cellHeight = Math.Min(slotPositionBottom - beforeCellEndPoint, listHeight - beforeCellEndPoint);
                    if (0 < cellHeight) {
                        String backgroundImageUrl = $"https://hayabusa.io/abema/programs/{slot.programs[0].id}/{slot.programs[0].providedInfo.thumbImg}.w280.h158.webp";
                        String backgroundImage = $"background-image: url({backgroundImageUrl});";
                        sb.Append($@"<tr><td class=""one-program-1""><div class=""one-program-2"" style=""height:calc(var(--one-min-height) * {cellHeight} - 1px );{backgroundImage}""><div class=""one-program-3"">{slot.getHTML()}</div></div></td></tr>");
                        beforeCellEndPoint += cellHeight;
                    }
                }
            }
            if (1 <= (listHeight - beforeCellEndPoint)) {
                sb.Append($@"<tr style=""height:calc( var(--one-min-height) *  {(listHeight - beforeCellEndPoint)} - 2px);background-color:red;""><td></td></tr>");
            }
            double fromUnixTimeSecond = new DateTimeOffset(from).ToUnixTimeSeconds();
            double toUnixTimeSecond = new DateTimeOffset(to).ToUnixTimeSeconds();
            String result = $@"<table rules=""all"" class=""vertical-table"" data-from-unix-second=""{fromUnixTimeSecond}"" data-to-unix-second=""{toUnixTimeSecond}"">{sb.ToString()}</table>";
            return result;
        }
    }
}
