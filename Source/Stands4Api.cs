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
    class Stands4Api {
        private readonly string Stands4UserID;
        private readonly string Stands4TokenID;
        private int MaxResultsToDisplay;

        public Stands4Api(string userID, string tokenID, int maxResults){
            Stands4UserID = userID;
            Stands4TokenID = tokenID;
            MaxResultsToDisplay = maxResults;
        }

        public void Calculate(CommandInfo info) {

            string args = info.Arguments.Replace("+", "%2B");
            string searchURL = "http://www.stands4.com/services/v2/conv.php?uid=" + Stands4UserID + "&tokenid=" + Stands4TokenID;
            searchURL += "&expression=" + args;

            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);

            var errs = xmlDoc.GetElementsByTagName("errorMessage");
            if (errs.Count > 0) {
                if (errs[0].InnerText.Length > 3) {
                    string errorText = errs[0].InnerText.Substring(3);
                    for (int i = 1; i < errs.Count; ++i)
                        errorText += " | " + errorText.Substring(3);
                    FattyBot.SendMessage(info.Source, String.Format("Error(s) returned: {0}", errorText));
                    return;
                }
                
            }

            StringBuilder messageAccumulator = new StringBuilder();
            var results = xmlDoc.GetElementsByTagName("result");
            if (results.Count > 0) {
                foreach (XmlNode result in results) {
                    if (!FattyBot.TryAppend(messageAccumulator, result.InnerText + "-", info.Source))
                        break;
                }
                messageAccumulator.Remove(messageAccumulator.Length - 1, 1);
                FattyBot.SendMessage(info.Source, messageAccumulator.ToString());
            }
            else
                FattyBot.SendMessage(info.Source, "No results for " + info.Arguments);
        }

        public void Acronym(CommandInfo info) {
            string searchURL = "http://www.stands4.com/services/v2/abbr.php?uid=" + Stands4UserID + "&tokenid=" + Stands4TokenID;
            searchURL += "&term=" + info.Arguments;
            // this is for exact lookup, so I don't have to display results in an intelligent manner
            searchURL += "&searchtype=e";

            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);

            var errs = xmlDoc.GetElementsByTagName("error");
            if (errs.Count > 0) {
                string errorText = errs[0].InnerText;
                for (int i = 1; i < errs.Count; ++i)
                    errorText += " | " + errorText;
                FattyBot.SendMessage(info.Source, String.Format("Errors returned: {0}", errorText));
                return;
            }

            StringBuilder messageAccumulator = new StringBuilder();

            var res = xmlDoc.GetElementsByTagName("result");
            int outputCount = 0;
            foreach (XmlNode elem in res) {
                var childRes = elem.ChildNodes;
                foreach (XmlNode child in childRes) {
                    if (child.Name != "definition")
                        continue;
                    string def = child.InnerText;
                    //iterate through rest of entries, even though there's no point, saves code.
                    if (outputCount >= MaxResultsToDisplay || !FattyBot.TryAppend(messageAccumulator, def + " | ", info.Source))
                        break;
                    ++outputCount;
                }
            }

            if (res.Count < 1)
                messageAccumulator.Append("No results for: " + info.Arguments);
            FattyBot.SendMessage(info.Source, messageAccumulator.ToString());
        }
    }
}
