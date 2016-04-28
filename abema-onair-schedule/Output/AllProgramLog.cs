using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace abema_onair_schedule.Output {
    class AllProgramLog {
        Dictionary<String, List<ScheduleDataset.Slot>> programsWithChId = new Dictionary<string, List<ScheduleDataset.Slot>>();
        public void addProgram(ScheduleDataset.Slot slot) {
            String chId = slot.channelId;
            if (this.programsWithChId.ContainsKey(chId) == false) {
                this.programsWithChId[chId] = new List<ScheduleDataset.Slot>();
            }
            this.programsWithChId[chId].Add(slot);
        }
        public void saveOneChannelLog(String path, String chId) {
            if (this.programsWithChId.ContainsKey(chId) == false) {
                return;
            }
            var startDate = getLastDateTime(path);
            var writeLogData = new List<ScheduleDataset.Slot>();
            foreach (var i in this.programsWithChId[chId]) {
                if (i.channelId != chId || i.startAt <= startDate) {
                    continue;
                }
                writeLogData.Add(i);
            }
            writeLogData.Sort((a, b) => {
                return a.startAt < b.startAt ? -1 : 1;
            });
            using (var fs = new System.IO.StreamWriter(path, true)) {
                StringBuilder sb = new StringBuilder();
                foreach (var i in writeLogData) {
                    sb.Append($"{i.startAt:yyyy/MM/dd(ddd)HH:mm} {(i.endAt - i.startAt).TotalMinutes:###}min\t{(i.mark.ContainsKey("first") ? "[初]" : "")}{i.title}\t{i.content.Replace("\r\n", "").Replace("\t", "")}");
                    sb.AppendLine();
                }
                fs.Write(sb.ToString());
            }
        }
        static DateTime getLastDateTime(String path) {
            DateTime result = new DateTime(1970, 1, 1, 0, 0, 0);
            if (System.IO.File.Exists(path) == false) {
                return result;
            }
            List<String> lines = System.IO.File.ReadLines(path, Encoding.UTF8).ToList();
            var reg = new System.Text.RegularExpressions.Regex(@"^(\d+)/(\d+)/(\d+)\(.+?\)(\d+):(\d+)");
            for (int i = 0; i < lines.Count; i++) {
                String line_ = lines[i];
                String line = line_.Trim();
                if (String.IsNullOrEmpty(line)) {
                    continue;
                }
                var match = reg.Match(line);
                if (match.Success == false) {
                    continue;
                }
                int y, m, d, h, mm;
                y = int.Parse(match.Groups[1].Value);
                m = int.Parse(match.Groups[2].Value);
                d = int.Parse(match.Groups[3].Value);
                h = int.Parse(match.Groups[4].Value);
                mm = int.Parse(match.Groups[5].Value);
                result = new DateTime(y, m, d, h, mm, 0);
            }
            return result;
        }
    }
}
