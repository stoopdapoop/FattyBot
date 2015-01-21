using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Net.Cache;

namespace FattyBot
{
    class BitBucketAPI
    {
        List<Tuple<string,string,string>> Subscriptions;
        List<Tuple<string, string, string>> Aliases;
        Value mostRecentValue = null;
        string AuthLogin;
        string AuthPassword;

        private class Result {
            public int pagelen { get; set; }
            public Value[] values { get; set; }
            
        }

        private class Value {
            public string hash { get; set; }
            public Author author { get; set; }
            public string message { get; set; }
            public DateTime date { get; set; }
        }

        private class Author {
            public User user { get; set; }
        }

        class User {
            public string Display_Name { get; set; }
            public string UserName { get; set; }
        }

        public BitBucketAPI(string[] subscriptionString, string[] bitBucketRepoAliases, string authLogin, string authPassword){
            Subscriptions = new List<Tuple<string, string, string>>();
            foreach (string str in subscriptionString) {
                var strParts = str.Split(' ');
                Subscriptions.Add(new Tuple<string, string, string>(strParts[0], strParts[1], strParts[2]));
            }
            Aliases = new List<Tuple<string,string,string>>();
            foreach (string str in bitBucketRepoAliases)
            {
                var strParts = str.Split(' ');
                Aliases.Add(new Tuple<string, string, string>(strParts[0], strParts[1], strParts[2]));
            }
            AuthLogin = authLogin;
            AuthPassword = authPassword;
        }

        public void RepoLink(CommandInput info)
        {
            try
            {
                if (info.Arguments.Length == 0)
                {
                    string failure = "Need to supply name of a repo, known repos are ";
                    Subscriptions.ForEach(x => failure += String.Format("\"{0}\", ", x.Item3));
                    failure = failure.Substring(0, failure.Length - 2);
                    FattyBot.SendMessage(info.Source, failure);
                    return;
                }

                bool found = false;

                string arg = info.Arguments.ToLower();
                Aliases.ForEach(x => { if (x.Item1 == arg) arg = x.Item3; });

                foreach (var sub in Subscriptions)
                {

                    if (sub.Item3.ToLower() == arg)
                    {
                        found = true;
                        FattyBot.SendMessage(info.Source, String.Format("https://bitbucket.org/{0}/{1}/", sub.Item2, sub.Item3));

                        string requestURL = "https://bitbucket.org/api/2.0/repositories/" + sub.Item2 + "/" + sub.Item3 + "/commits/";
                        HttpWebRequest searchRequest = HttpWebRequest.Create(requestURL) as HttpWebRequest;
                        string authInfo = String.Format("{0}:{1}", AuthLogin, AuthPassword);
                        authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                        searchRequest.Headers["Authorization"] = "Basic " + authInfo;

                        HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
                        StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
                        string wholeMessage = reader.ReadToEnd();

                        Result res = JsonConvert.DeserializeObject<Result>(wholeMessage);

                        if (res.values[0] != null)
                        {
                            TimeSpan lastCommit = DateTime.Now - res.values[0].date;
                            FattyBot.SendMessage(info.Source, string.Format("last commit was {0} ago. ({1:yyyy-MM-dd HH:mm:ss})", FattyBot.GetPrettyTime(lastCommit), res.values[0].date));
                        }
                        else
                            FattyBot.SendMessage(info.Source, "Don't see any previous commits");
                    }
                }
                if (!found)
                {
                    string failure = String.Format("No known repo named {0}, tracked repos are ", info.Arguments);
                    Subscriptions.ForEach(x => failure += x.Item3 + ", ");
                    FattyBot.SendMessage(info.Source, failure);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void CheckRepos(object state) {
            try {
                foreach (var sub in Subscriptions) {
                    string requestURL = "https://bitbucket.org/api/2.0/repositories/" + sub.Item2 + "/" + sub.Item3 + "/commits/";
                    HttpWebRequest searchRequest = HttpWebRequest.Create(requestURL) as HttpWebRequest;
                    string authInfo = String.Format("{0}:{1}", AuthLogin, AuthPassword);
                    authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                    searchRequest.Headers["Authorization"] = "Basic " + authInfo;

                    HttpWebResponse searchResponse = searchRequest.GetResponse() as HttpWebResponse;
                    StreamReader reader = new StreamReader(searchResponse.GetResponseStream());
                    string wholeMessage = reader.ReadToEnd();

                    Result res = JsonConvert.DeserializeObject<Result>(wholeMessage);
                    if (res.values[0] == null)
                        return;

                    if (mostRecentValue == null)
                        mostRecentValue = res.values[0];

                    if (mostRecentValue.date < res.values[0].date) {
                        int i = 0;
                        FattyBot.SendMessage(sub.Item1, String.Format("Commit by {0}:", res.values[i].author.user.UserName));
                        while (res.values[i].date > mostRecentValue.date) {
                            FattyBot.SendMessage(sub.Item1, String.Format("- {0}", res.values[i].message));
                            ++i;
                        }
                        FattyBot.SendMessage(sub.Item1, String.Format("https://bitbucket.org/{0}/{1}/commits/all", sub.Item2, sub.Item3));
                        mostRecentValue = res.values[0];
                    }
                }
            } catch (Exception ex){
                Console.WriteLine(ex.Message);
            }
            
        }
    }
}
