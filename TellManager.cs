using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyBot {
    class TellManager {
        private Dictionary<string, List<Tuple<String, DateTime, String>>> TellList = new Dictionary<string, List<Tuple<String, DateTime, String>>>();
        private UserAliasesRegistry TellAliasRegistry;

        public TellManager(UserAliasesRegistry aliases) {
            TellAliasRegistry = aliases;
        }

        public bool GetTellsForUser(string name, out List<Tuple<String, DateTime, String>> tells) {

            tells = new List<Tuple<String, DateTime, String>>();

            UserAliasGroup TestAliasGroup;

            if (TellAliasRegistry.GetAliasGroup(name, out TestAliasGroup)) {
                var AliasIterator = TestAliasGroup.GetUserAliases();
                foreach (string aliasName in AliasIterator) {
                    List<Tuple<String, DateTime, String>> thisTellList;
                    if(TellList.TryGetValue(aliasName, out thisTellList)){
                        tells.AddRange(thisTellList);
                        TellList.Remove(aliasName);
                    }
                }
                if (tells.Count > 0)
                    return true;
                else
                    return false;
            }
            else {
                List<Tuple<String, DateTime, String>> thisTellList;
                if (TellList.TryGetValue(name, out thisTellList)) {
                    tells = thisTellList;
                    TellList.Remove(name);
                    return true;
                }
                else {
                    return false;
                }
            }
        }

        public void AddTellForUser(string to, string from, string message) {
            List<Tuple<String, DateTime, String>> thisTell;
            bool isPresent = TellList.TryGetValue(to, out thisTell);
            if (isPresent)
                thisTell.Add(new Tuple<String, DateTime, String>(from, DateTime.Now, message));
            else
                TellList[to] = new List<Tuple<String, DateTime, string>>(new Tuple<String, DateTime, string>[] { new Tuple<String, DateTime, string>(from, DateTime.Now, message) }); 
        }
    }
}
