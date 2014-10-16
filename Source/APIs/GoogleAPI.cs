﻿using System;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace FattyBot {
    class GoogleAPI {

        private readonly string GoogleAPIKey;
        private readonly string GoogleCustomSearchID;
       
        #region GoogleStructs
        private class GoogleSearchItem {
            public string kind { get; set; }
            public string title { get; set; }
            public string link { get; set; }
            public string displayLink { get; set; }
        }

        private class ShortURL {
            public string id { get; set; }
        }

        private class SourceUrl {
            public string type { get; set; }
            public string template { get; set; }
        }

        private class GoogleSearchResults {
            public string kind { get; set; }
            public SourceUrl url { get; set; }
            public GoogleSearchItem[] items { get; set; }
        }
        #endregion

        #region Private Methods

        #endregion

        #region Public Commands

        public GoogleAPI(string apiKey, string customSearchID) {
            this.GoogleAPIKey = apiKey;
            this.GoogleCustomSearchID = customSearchID;
        }

        public void Google(CommandInput info) {
            string searchURL = "https://www.googleapis.com/customsearch/v1?key=" + GoogleAPIKey + "&cx=" + GoogleCustomSearchID + "&q=";
            searchURL += info.Arguments;
            GoogleAPIPrinter(searchURL, info.Source);
        }

        public void GoogleImageSearch(CommandInput info) {
            string searchURL = "https://www.googleapis.com/customsearch/v1?key=" + GoogleAPIKey + "&cx=" + GoogleCustomSearchID + "&searchType=image&q=";
            searchURL += info.Arguments;
            GoogleAPIPrinter(searchURL, info.Source);
        }

        public void URLShortener(CommandInput info) {

            string shortURL = GetShortURL(info.Arguments);

            FattyBot.SendMessage(info.Source, shortURL);
        }

        public string GetShortURL(string longURL) {
            string searchURL = "https://www.googleapis.com/urlshortener/v1/url?key=" + GoogleAPIKey;

            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;

            ASCIIEncoding encoder = new ASCIIEncoding();
            string pls = JsonConvert.SerializeObject(new { longUrl = longURL });
            byte[] data = encoder.GetBytes(pls);

            searchRequest.ContentType = "application/json";
            searchRequest.ContentLength = data.Length;
            searchRequest.Expect = "application/json";
            searchRequest.Method = "POST";
            searchRequest.GetRequestStream().Write(data, 0, data.Length);
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());

            ShortURL temp = JsonConvert.DeserializeObject<ShortURL>(reader.ReadToEnd());

            return temp.id;
        }
        #endregion

        #region utils
        private void GoogleAPIPrinter(string searchURL, string source) {
            HttpWebRequest searchRequest = HttpWebRequest.Create(searchURL) as HttpWebRequest;
            HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
            StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
            String data = reader.ReadToEnd();

            GoogleSearchResults results = JsonConvert.DeserializeObject<GoogleSearchResults>(data);
            StringBuilder messageAccumulator = new StringBuilder();
            int i = 0;
            int messageOverhead = FattyBot.GetMessageOverhead(source);
            while (i < 10 && results.items != null && i < results.items.Length) {
                if (results.items.Length >= i) {
                    StringBuilder resultAccumulator = new StringBuilder();
                    GoogleSearchItem resultIterator = results.items[i++];
                    resultAccumulator.Append(String.Format("\"{0}\"", resultIterator.title));
                    resultAccumulator.Append(" - ");
                    resultAccumulator.Append(String.Format("\x02{0}\x02", resultIterator.link));
                    resultAccumulator.Append(@" 4| ");
                    if (messageAccumulator.Length + resultAccumulator.Length + messageOverhead <= 480)
                        messageAccumulator.Append(resultAccumulator);
                    else
                        break;
                }
                else {
                    break;
                }
            }
            if (messageAccumulator.Length == 0)
                FattyBot.SendMessage(source, "No Results Found");
            else
                FattyBot.SendMessage(source, messageAccumulator.ToString());
        }
        #endregion

    }
}
