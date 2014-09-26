using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace FattyBot {
    class WolframAPI {
        const string WolframAlphaKey = "95JE4A-XQLX9WPU99";

        private List<DateTime> RecentMathInvocations = new List<DateTime>();

        public void MathLimit(string caller, string args, string source) {
            //cull old messages
            TimeSpan anHour = new TimeSpan(1, 0, 0);
            for (int i = RecentMathInvocations.Count - 1; i >= 0; --i) {
                if ((DateTime.Now - RecentMathInvocations[i]) > anHour)
                    RecentMathInvocations.RemoveAt(i);
            }
            FattyBot.SendMessage(source, String.Format("{0} wolfram invocations have been made in the past hour. {1} left.", RecentMathInvocations.Count, 30 - RecentMathInvocations.Count));
        }

        public void Math(string caller, string args, string source) {

            //cull old messages
            TimeSpan anHour = new TimeSpan(1, 0, 0);
            for (int i = RecentMathInvocations.Count - 1; i >= 0; --i) {
                if ((DateTime.Now - RecentMathInvocations[i]) > anHour)
                    RecentMathInvocations.RemoveAt(i);
            }

            if (RecentMathInvocations.Count > 30) {
                TimeSpan nextInvoke = anHour - (DateTime.Now - RecentMathInvocations[0]);
                FattyBot.SendMessage(source, String.Format("Sorry {0}, rate limit on this command exceeded, you can use it again in {2} minutes", caller, nextInvoke.Minutes));
                return;
            }

            args = args.Replace("+", "%2B");
            string searchURL = "http://api.wolframalpha.com/v2/query?input=" + args + "&appid=" + WolframAlphaKey;
            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            //string butts = searchRequest.RequestUri.IsWellFormedOriginalString();
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);
            StringBuilder messageAccumulator = new StringBuilder();
            int messageOverhead = FattyBot.GetMessageOverhead(source);

            XmlNodeList res = xmlDoc.GetElementsByTagName("queryresult");
            if (res[0].Attributes["success"].Value == "false") {
                messageAccumulator.Append("Query failed: ");
                res = xmlDoc.GetElementsByTagName("tip");
                for (int i = 0; i < res.Count; i++) {
                    string desc = res[i].InnerText;
                    if (desc.Length + messageOverhead + messageAccumulator.Length <= 480)
                        messageAccumulator.Append(desc + ". ");
                    else
                        break;
                }
                res = xmlDoc.GetElementsByTagName("didyoumean");

                for (int i = 0; i < res.Count; i++) {
                    string desc = "";
                    if (i == 0)
                        desc += "Did you mean: ";
                    desc += res[i].InnerText + " ? ";
                    if (desc.Length + messageOverhead + messageAccumulator.Length <= 480)
                        messageAccumulator.Append(desc);
                    else
                        break;
                }
                FattyBot.SendMessage(source, messageAccumulator.ToString());
            }
            else {
                res = xmlDoc.GetElementsByTagName("plaintext");

                for (int i = 0; i < res.Count; i++) {
                    string value = res[i].InnerText;
                    string description = res[i].ParentNode.ParentNode.Attributes["title"].Value;
                    if (description == "Number line")
                        continue;
                    description = description + ":" + value;
                    if (description.Length + messageOverhead + messageAccumulator.Length <= 480)
                        messageAccumulator.Append(description + " / ");
                    else
                        break;
                }
                messageAccumulator.Remove(messageAccumulator.Length - 2, 1);
                messageAccumulator.Replace("\n", " ");
                messageAccumulator.Replace("\r", " ");

                FattyBot.SendMessage(source, messageAccumulator.ToString());
            }

            RecentMathInvocations.Add(DateTime.Now);
        }
    }
}
