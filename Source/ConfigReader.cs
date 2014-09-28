using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FattyBot {
    class ConfigReader {

        private Dictionary<string, string> Defines;
        public ConfigReader() {
            Defines = new Dictionary<string, string>();
        }

        public void AddConfig(string filePath){
            //todo: catch fnf errors
            StreamReader sr = new StreamReader(filePath);

            string line;
            while((line = sr.ReadLine()) != null){
                if (line[0] == '!')
                    continue;
                int spacePos = line.IndexOf("=");
                string defineKey = line.Substring(0, spacePos);
                string defineValue = line.Substring(spacePos + 1, line.Length - (spacePos + 1));
                Defines.Add(defineKey, defineValue);
            }
        }

        public string GetValue(string key) {
            string value;
            bool found = Defines.TryGetValue(key, out value);
            if (found)
                return value;
            else
                return "";
        }
    }
}
