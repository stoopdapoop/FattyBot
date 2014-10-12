using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Net;
using System.IO;

namespace FattyBot {
    class WolframAPI {
        private readonly string WolframAlphaKey;
        private int MaxCallsPerHour = 30;

        private List<DateTime> RecentMathInvocations = new List<DateTime>();

        public WolframAPI(string apiKey, int maxCallsPerHour) {
            WolframAlphaKey = apiKey;
            MaxCallsPerHour = maxCallsPerHour;
        }

        public void MathLimit(CommandInfo info) {
            //cull old messages
            TimeSpan anHour = new TimeSpan(1, 0, 0);
            for (int i = RecentMathInvocations.Count - 1; i >= 0; --i) {
                if ((DateTime.Now - RecentMathInvocations[i]) > anHour)
                    RecentMathInvocations.RemoveAt(i);
            }
            FattyBot.SendMessage(info.Source, String.Format("{0} wolfram invocations have been made in the past hour. {1} left.", RecentMathInvocations.Count, 30 - RecentMathInvocations.Count));
        }

        public void Math(CommandInfo info) {

            //cull old messages
            TimeSpan anHour = new TimeSpan(1, 0, 0);
            for (int i = RecentMathInvocations.Count - 1; i >= 0; --i) {
                if ((DateTime.Now - RecentMathInvocations[i]) > anHour)
                    RecentMathInvocations.RemoveAt(i);
            }

            if (RecentMathInvocations.Count > MaxCallsPerHour) {
                TimeSpan nextInvoke = anHour - (DateTime.Now - RecentMathInvocations[0]);
                FattyBot.SendMessage(info.Source, String.Format("Sorry {0}, rate limit on this command exceeded, you can use it again in {2} minutes", info.Caller, nextInvoke.Minutes));
                return;
            }

            string args = info.Arguments.Replace("+", "%2B");
            string searchURL = "http://api.wolframalpha.com/v2/query?input=" + args + "&appid=" + WolframAlphaKey;
            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            //string butts = searchRequest.RequestUri.IsWellFormedOriginalString();
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);
            StringBuilder messageAccumulator = new StringBuilder();
            int messageOverhead = FattyBot.GetMessageOverhead(info.Source);

            XmlNodeList res = xmlDoc.GetElementsByTagName("queryresult");
            if (res[0].Attributes["success"].Value == "false") {
                messageAccumulator.Append("Query failed: ");
                res = xmlDoc.GetElementsByTagName("tip");
                for (int i = 0; i < res.Count; i++) {
                    string desc = res[i].Attributes["text"].Value;
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
                FattyBot.SendMessage(info.Source, messageAccumulator.ToString());
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
                if (messageAccumulator.Length > 0) {
                    messageAccumulator.Remove(messageAccumulator.Length - 1, 1);
                    messageAccumulator.Replace("\n", " ");
                    messageAccumulator.Replace("\r", " ");
                    FattyBot.SendMessage(info.Source, messageAccumulator.ToString());
                }
                else {
                    FattyBot.SendMessage(info.Source, "The result was likely too big to fit into a single message. Sorry " + info.Caller + " :[");
                }
            }

            RecentMathInvocations.Add(DateTime.Now);
        }
    }
}
