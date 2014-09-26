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

        private const string DictionaryKey = "e38f6db8-e792-44a6-b3ab-9acc75e9edec";
        private const string ThesaurusKey = "3ce55c4e-5f26-4cce-af61-1dff08836aa7";
        private GoogleAPI GoogleInterface;

        public MerriamWebsterAPI(GoogleAPI goog) {
            GoogleInterface = goog;
        }

        public void Dictionary(string caller, string args, string source) {
            string searchURL = "http://www.dictionaryapi.com/api/v1/references/collegiate/xml/" + args + "?key=" + DictionaryKey;
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
                string shortUrl = GoogleInterface.GetShortURL("http://www.merriam-webster.com/dictionary/" + args);
                var chldrn = res[0].ChildNodes;
                foreach (XmlNode chld in chldrn) {
                    if (chld.Name == "def") {
                        var defnodes = chld.ChildNodes;
                        foreach (XmlNode def in defnodes) {
                            if (def.Name == "dt") {
                                candidateMessage += def.InnerText + " | ";
                                if (!FattyBot.TryAppend(messageAccumulator, candidateMessage, source, shortUrl.Length + 2)) {
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
                        if (!FattyBot.TryAppend(messageAccumulator, nd.InnerText + "|", source))
                            break;
                    }
                    messageAccumulator.Remove(messageAccumulator.Length - 2, 1);
                    messageAccumulator.Append("?");
                }
            }

            FattyBot.SendMessage(source, messageAccumulator.ToString());
        }
    }
}
