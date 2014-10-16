using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FattyBot {
    class ConfigReader {

        private Dictionary<string, string> Defines;
        private Dictionary<string, string[]> DefineArrays;

        public ConfigReader() {
            Defines = new Dictionary<string, string>();
            DefineArrays = new Dictionary<string, string[]>();
        }

        public void AddConfig(string filePath){
            //todo: catch fnf errors
            StreamReader sr = new StreamReader(filePath);

            string line;
            while((line = sr.ReadLine()) != null){
                if (line[0] == '!' || line.Length == 0)
                    continue;

                if (line[0] == '<') {
                    int assignPos = line.IndexOf("=");
                    string defineKey = line.Substring(1, assignPos-1);

                    List<string> defineValue = new List<string>();
                    defineValue.Add(line.Substring(assignPos + 1, line.Length - (assignPos + 1)));                   
                    while ((line = sr.ReadLine()) != null && line != ">"){
                        defineValue.Add(line);
                    }
                    DefineArrays.Add(defineKey, defineValue.ToArray());
                }
                else {
                    int assignPos = line.IndexOf("=");
                    string defineKey = line.Substring(0, assignPos);
                    string defineValue = line.Substring(assignPos + 1, line.Length - (assignPos + 1));
                    Defines.Add(defineKey, defineValue);
                }                
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

        public string[] GetValueArray(string key) {
            string[] value;
            bool found = DefineArrays.TryGetValue(key, out value);
            if (found)
                return value;
            else
                return new string[]{""};
        }
    }
}
