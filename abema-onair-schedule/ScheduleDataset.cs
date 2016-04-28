using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace abema_onair_schedule.ScheduleDataset {
    class ScheduleDataset {
        public List<String> availableDates { get; set; } = new List<string>();
        public List<ChannelSchedule> channelSchedules { get; set; } = new List<ChannelSchedule>();
        public List<Channel> channels { get; set; } = new List<Channel>();
    }
    class Credit {
        public List<String> casts { get; set; } = new List<string>();
        public List<String> copyrights { get; set; } = new List<string>();
        public List<String> crews { get; set; } = new List<string>();
    }
    class Episode {
        public String content { get; set; } = "";
        public String name { get; set; } = "";
        public String overview { get; set; } = "";
        public String sequence { get; set; } = "";
        public String title { get; set; } = "";

    }
    class ProvidedInfo {
        // https://hayabusa.io/abema/programs/90-139zxndekzgfg_s0_p1/thumb001.webp
        public List<String> sceneThumbImgs { get; set; } = new List<string>();
        public String thumbImg { get; set; } = "";
    }
    class ThemeColor {
        public String background { get; set; } = "";
        public String detail { get; set; } = "";
        public String primary { get; set; } = "";
        public String secondary { get; set; } = "";
        public override string ToString() {
            return $"background={this.background} detail={this.detail} primary={this.primary} secondary={this.secondary}";
        }
    }
    class Series {
        public String id { get; set; } = "";
        public ThemeColor themeColor { get; set; } = new ThemeColor();
    }
    class Program {
        public Credit credit { get; set; } = new Credit();
        public Episode episode { get; set; } = new Episode();
        public String id { get; set; } = "";
        public ProvidedInfo providedInfo { get; set; } = new ProvidedInfo();
        public Series series { get; set; } = new Series();
    }
    class Slot {
        [JsonIgnore]
        public Boolean MarkFirst {
            get {
                if (this.mark.ContainsKey("first") && this.mark["first"] == true) {
                    return true;
                } else {
                    return false;
                }
            }
        }
        [JsonIgnore]
        public TimeSpan Duration {
            get {
                return this.endAt - this.startAt;
            }
        }
        public String channelId { get; set; } = "";
        public String content { get; set; } = "";
        public String displayProgramId { get; set; } = "";
        public Dictionary<String, Boolean> flags { get; set; } = new Dictionary<string, Boolean>();
        public String highlight { get; set; } = "";
        public String id { get; set; } = "";
        public Dictionary<String, Boolean> mark { get; set; } = new Dictionary<string, Boolean>();
        public List<Program> programs { get; set; } = new List<Program>();
        [JsonConverter(typeof(UnixTimeSecondConverter))]
        public DateTime startAt { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0).LocalDateTime;
        [JsonConverter(typeof(UnixTimeSecondConverter))]
        public DateTime endAt { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0).LocalDateTime;
        [JsonConverter(typeof(UnixTimeSecondConverter))]
        public DateTime tableStartAt { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0).LocalDateTime;
        [JsonConverter(typeof(UnixTimeSecondConverter))]
        public DateTime tableEndAt { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0).LocalDateTime;
        public String tableHighlight { get; set; } = "";
        [JsonConverter(typeof(UnixTimeSecondConverter))]
        public DateTime timeshiftEndAt { get; set; } = DateTimeOffset.FromUnixTimeSeconds(0).LocalDateTime;
        public String title { get; set; } = "";
        public String getHTML() {
            StringBuilder sb = new StringBuilder();
            sb.Append($@"<span class=""startTime"">{startAt:HH:mm}</span>");
            if (this.mark.ContainsKey("first")) {
                sb.Append($@"<span class=""mark-first"">初</span>");
            }
            sb.Append($@"<a href=""https://abema.tv/channels/{this.channelId}/slots/{this.id}""><span class=""title"">{this.title}</span></a>");
            sb.Append("<br>");
            // タイトルごとのカバー画像か、エピソードごとの画像か
            sb.Append($@"<img class=""program-thumbnail"" src=""https://hayabusa.io/abema/series/{this.programs[0].series.id}/cover.w500.webp"">");
            //sb.Append($@"<img class=""program-thumbnail"" src=""https://hayabusa.io/abema/programs/{this.programs[0].id}/{this.programs[0].providedInfo.thumbImg}.w280.h158.webp"">");
            sb.Append($@"<span class=""content"">{this.content}</span>");
            return sb.ToString();
        }
        public override string ToString() {
            return $"{channelId} {startAt.ToString("yyyy-MM-dd(ddd)HH:mm", CultureInfo.GetCultureInfo("en-us"))}～ {this.title}";
        }
    }
    class ChannelSchedule {
        public String channelId { get; set; } = "";
        public String date { get; set; } = "";
        public List<Slot> slots { get; set; } = new List<Slot>();
        public override string ToString() {
            return $"{this.date} {this.channelId} sloats=[{String.Join(" , ", this.slots)}]";
        }
    }
    class Channel {
        public String id { get; set; } = "";
        public String name { get; set; } = "";
        public int order { get; set; } = 0;
        public override string ToString() {
            return $"{id} {name} ({order})";
        }
    }
    public class UnixTimeSecondConverter : DateTimeConverterBase {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteRawValue(new DateTimeOffset((DateTime)value).ToUnixTimeSeconds().ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.Value == null) { return null; }
            return DateTimeOffset.FromUnixTimeSeconds((long)reader.Value).LocalDateTime;
        }
    }
}
