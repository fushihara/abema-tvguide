using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace abema_onair_schedule.Output {
    class ReportChannel {
        class ScheduleAndEpisodeSet : IComparable<ScheduleAndEpisodeSet> {
            public DateTime firstStartAt = new DateTime(2100, 1, 1);
            public int totalScheduleCount = 0;
            public int isFirstMarkScheduleCount = 0;
            public String baseTitle = "";
            public List<ScheduleDataset.Slot> slots = new List<ScheduleDataset.Slot>();
            public ScheduleAndEpisodeSet(String baseTitle) {
                this.baseTitle = baseTitle;
            }
            public void addSlot(ScheduleDataset.Slot slot) {

                this.firstStartAt = slot.startAt < this.firstStartAt ? slot.startAt : this.firstStartAt;
                this.totalScheduleCount++;
                if (slot.MarkFirst) {
                    this.isFirstMarkScheduleCount++;
                }
                this.slots.Add(slot);
            }
            public String getText() {
                var sb = new StringBuilder();
                sb.AppendLine($"{this.baseTitle}");
                var slots = new List<ScheduleDataset.Slot>();
                foreach (var j in this.slots) {
                    String datetime = j.startAt.ToString("yyyy-MM-dd(ddd)HH:mm") + " ～ " + j.endAt.ToString("HH:mm");
                    String firstMark = "     ";
                    if (j.MarkFirst) {
                        firstMark = "[初] ";
                    }
                    String shortTitle = j.title;
                    shortTitle = shortTitle.Replace(baseTitle, "");
                    shortTitle = shortTitle.Trim();
                    String content = j.content;
                    content = content.Replace("\r\n", "").Replace("\t", "");
                    sb.Append($"    {datetime} {j.channelId} {firstMark}{shortTitle} {content}");
                    sb.AppendLine();
                }
                return sb.ToString();
            }
            public int CompareTo(ScheduleAndEpisodeSet other) {
                if (this.firstStartAt == other.firstStartAt) {
                    return 0;
                } else if (this.firstStartAt < other.firstStartAt) {
                    return 1;
                } else {
                    return -1;
                }
            }
            public override string ToString() {
                return firstStartAt + " " + baseTitle;
            }
        }
        Dictionary<String, List<ScheduleDataset.Slot>> programsWithChId = new Dictionary<string, List<ScheduleDataset.Slot>>();
        public void addProgram(ScheduleDataset.Slot slot) {
            String chId = slot.channelId;
            if (this.programsWithChId.ContainsKey(chId) == false) {
                this.programsWithChId[chId] = new List<ScheduleDataset.Slot>();
            }
            this.programsWithChId[chId].Add(slot);
        }
        public void saveChannelLog(String path, params String[] chids) {
            {
                Boolean hasChData = false;
                foreach (var i in chids) {
                    hasChData = this.programsWithChId.ContainsKey(i) ? true : hasChData;
                }
                if (hasChData == false) {
                    return;
                }
            }
            var writeLogData = new List<ScheduleDataset.Slot>();
            var filterDate = DateTime.Now.AddHours(-24);
            foreach (var i in chids) {
                foreach (var j in this.programsWithChId[i]) {
                    if (j.startAt < filterDate) {
                        continue;
                    }
                    writeLogData.Add(j);
                }
            }
            writeLogData.Sort((a, b) => {
                return a.startAt < b.startAt ? -1 : 1;
            });

            // タイトルの一覧を作成する
            var hasFirstFlagTitles = new List<string>();
            var allTitles = new List<string>();
            {
                var titles = getTitles(writeLogData);
                allTitles = titles.Item1;
                hasFirstFlagTitles = titles.Item2;
            }

            var scheduleAndEpisodeSetHasFirstMark = new List<ScheduleAndEpisodeSet>();
            var scheduleAndEpisodeSetNormal = new List<ScheduleAndEpisodeSet>();

            foreach (var i in allTitles) {
                if (hasFirstFlagTitles.Contains(i)) {
                    var saes = new ScheduleAndEpisodeSet(i);
                    writeLogData.FindAll((a) => { return trimEpisodeNumber(a.title) == i; })
                                .ForEach(a => { saes.addSlot(a); });
                    scheduleAndEpisodeSetHasFirstMark.Add(saes);
                } else {
                    var saes = new ScheduleAndEpisodeSet(i);
                    writeLogData.FindAll((a) => { return trimEpisodeNumber(a.title) == i; })
                                .ForEach(a => { saes.addSlot(a); });
                    scheduleAndEpisodeSetNormal.Add(saes);
                }
            }
            scheduleAndEpisodeSetHasFirstMark.Sort();
            scheduleAndEpisodeSetNormal.Sort();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("★新エピソードあり");
            scheduleAndEpisodeSetHasFirstMark.ForEach(i => { sb.AppendLine(i.getText()); });
            sb.AppendLine();
            sb.AppendLine("★継続タイトル");
            scheduleAndEpisodeSetNormal.ForEach(i => { sb.AppendLine(i.getText()); });
            sb.AppendLine();
            System.IO.File.WriteAllText(path, sb.ToString());
        }
        String trimEpisodeNumber(String title) {
            var numbers = new System.Text.RegularExpressions.Regex(@"#\d+[~〜]#?\d+");
            var number = new System.Text.RegularExpressions.Regex(@"[#♯]\d+");
            title = numbers.Replace(title, "");
            title = title.Trim();
            title = number.Replace(title, "");
            title = title.Trim();
            return title;
        }
        Tuple<List<string>, List<String>> getTitles(List<ScheduleDataset.Slot> slots) {
            List<String> allTitles = new List<string>();
            List<String> hasFirstFlagTitles = new List<string>();
            var numbers = new System.Text.RegularExpressions.Regex(@"#\d+〜#?\d+");
            var number = new System.Text.RegularExpressions.Regex(@"[#♯]\d+");
            foreach (var i in slots) {
                String title = trimEpisodeNumber(i.title);
                if (allTitles.Contains(title) == false) {
                    allTitles.Add(title);
                }
                if (i.MarkFirst && hasFirstFlagTitles.Contains(title) == false) {
                    hasFirstFlagTitles.Add(title);
                }
            }
            return new Tuple<List<string>, List<string>>(allTitles, hasFirstFlagTitles);
        }
    }
}
