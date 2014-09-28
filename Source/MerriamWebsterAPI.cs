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
    class MerriamWebsterAPI {

        private readonly string DictionaryKey;
        private readonly string ThesaurusKey;
        private GoogleAPI GoogleInterface;

        public MerriamWebsterAPI(string dictionaryKey, string thesaurusKey, GoogleAPI goog) {
            DictionaryKey = dictionaryKey;
            ThesaurusKey = thesaurusKey;
            GoogleInterface = goog;
        }

        public void Dictionary(CommandInfo info) {
            string searchURL = "http://www.dictionaryapi.com/api/v1/references/collegiate/xml/" + info.Arguments + "?key=" + DictionaryKey;
            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(reader);
            StringBuilder messageAccumulator = new StringBuilder();

            var res = xmlDoc.GetElementsByTagName("entry");
            if (res.Count > 0) {
                messageAccumulator.Append(res.Count + " result(s): ");
                string candidateMessage = res[0].Attributes["id"].Value;
                string shortUrl = GoogleInterface.GetShortURL("http://www.merriam-webster.com/dictionary/" + info.Arguments);
                var chldrn = res[0].ChildNodes;
                foreach (XmlNode chld in chldrn) {
                    if (chld.Name == "def") {
                        var defnodes = chld.ChildNodes;
                        foreach (XmlNode def in defnodes) {
                            if (def.Name == "dt") {
                                candidateMessage += def.InnerText + " | ";
                                if (!FattyBot.TryAppend(messageAccumulator, candidateMessage, info.Source, shortUrl.Length + 2)) {
                                    break;
                                }
                            }
                        }
                    }
                }
                messageAccumulator.Append(" " + shortUrl);
            }
            else {
                messageAccumulator.Append("No results found. ");
                res = xmlDoc.GetElementsByTagName("suggestion");

                if (res.Count > 0) {
                    messageAccumulator.Append("Did you mean: ");
                    foreach (XmlNode nd in res) {
                        if (!FattyBot.TryAppend(messageAccumulator, nd.InnerText + "|", info.Source))
                            break;
                    }
                    messageAccumulator.Remove(messageAccumulator.Length - 1, 1);
                    messageAccumulator.Append("?");
                }
            }

            FattyBot.SendMessage(info.Source, messageAccumulator.ToString());
        }
    }
}
