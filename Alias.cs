using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyBot {
    public class UserAliasesRegistry {
            private Dictionary<string, UserAliasGroup> AliasRegistry = new Dictionary<string, UserAliasGroup>();

            public bool GetAliasGroup(string nick, out UserAliasGroup aliasGroup) {
                return AliasRegistry.TryGetValue(nick, out aliasGroup);
            }

            public string AddAlias(string newAlias, string oldName) {
                UserAliasGroup testGroup;
                bool newKeyExists = AliasRegistry.TryGetValue(newAlias, out testGroup);
                if(newKeyExists)
                    return String.Format("That alias is already assigned to {0}", testGroup.GetUserAliases().First());
 
                bool oldKeyExists = AliasRegistry.TryGetValue(oldName, out testGroup);
                if(!oldKeyExists)
                {
                    UserAliasGroup newAliasGroup = new UserAliasGroup(oldName, newAlias);
                    AliasRegistry.Add(newAlias, newAliasGroup);
                    AliasRegistry.Add(oldName, newAliasGroup);
                    return String.Format("New alias group created for {0} that includes {1}", oldName, newAlias);
                }
                else{
                    if(testGroup.AddAlias(newAlias)) {
                        AliasRegistry.Add(newAlias, testGroup);
                        return String.Format("Alias {0} added to group that includes {1}", newAlias, oldName);
                    }
                }
                
                return "Something very bad has happened";
            }
        }

        public class UserAliasGroup {
            private HashSet<string> Aliases;

            public UserAliasGroup(string oldname, string newAlias) {
                Aliases = new HashSet<string>( new string[] { oldname, newAlias });                
            }

            public bool AddAlias(string alias) {
                return Aliases.Add(alias);
            }

            public IEnumerable<string> GetUserAliases() {
                return Aliases.AsEnumerable();
            }
            
        }
}
