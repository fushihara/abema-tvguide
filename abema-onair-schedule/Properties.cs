using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace abema_onair_schedule {
    class Properties {
        public String jsonLogDirectory = null;
        public String programAllDirectory = null;
        public String programChannelAllDirectory = null;
        public String programLaterDirectory = null;
        public String programChannelLaterDirectory = null;
        public String programAllLogDirectory = null;
        public String programChannelAllLogDirectory = null;
        public String programLaterLogDirectory = null;
        public String programChannelLaterLogDirectory = null;
        public String allProgramCsvLogDirectory = null;
        public String reprtProgramDirectory = null;
        public Properties() {
            String iniPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            iniPath = System.IO.Path.ChangeExtension(iniPath, "ini");

            if (System.IO.File.Exists(iniPath) == false) {
                String val;
                System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (System.IO.StreamReader sr = new System.IO.StreamReader(myAssembly.GetManifestResourceStream(@"abema_onair_schedule.Resources.default-ini.ini"), System.Text.Encoding.UTF8)) {
                    val = sr.ReadToEnd();
                }
                System.IO.File.WriteAllText(iniPath, val.Trim());
            }
            List<String> lines = System.IO.File.ReadLines(iniPath, Encoding.UTF8).ToList();
            for (int i = 0; i < lines.Count; i++) {
                String line_ = lines[i];
                String line = line_.Trim();
                if (line == "" || line.StartsWith(";") || line.StartsWith("#") || !line.Contains("=")) {
                    continue;
                }
                String key, value;
                key = line.Substring(0, line.IndexOf("=")).Trim();
                value = line.Substring(line.IndexOf("=") + 1).Trim();
                switch (key) {
                    case "jsonLogDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.jsonLogDirectory = value;
                        break;
                    case "programAllDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.programAllDirectory = value;
                        break;
                    case "programChannelAllDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.programChannelAllDirectory = value;
                        break;
                    case "programLaterDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.programLaterDirectory = value;
                        break;
                    case "programChannelLaterDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.programChannelLaterDirectory = value;
                        break;

                    case "programAllLogDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.programAllLogDirectory = value;
                        break;
                    case "programChannelAllLogDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.programChannelAllLogDirectory = value;
                        break;
                    case "programLaterLogDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.programLaterLogDirectory = value;
                        break;
                    case "programChannelLaterLogDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.programChannelLaterLogDirectory = value;
                        break;
                    case "allProgramCsvLogDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.allProgramCsvLogDirectory = value;
                        break;
                    case "reprtProgramDirectory":
                        System.IO.Directory.CreateDirectory(new System.IO.FileInfo(value).FullName);
                        this.reprtProgramDirectory = value;
                        break;
                }
            }
        }
    }
}
