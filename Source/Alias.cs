using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyBot {

    public class AliasAPI
    {
        public UserAliasesRegistry FattyUserAliases;
        private DatabaseManager DatabaseInterface;

        public AliasAPI(DatabaseManager db)
        {
            FattyUserAliases = new UserAliasesRegistry();
            DatabaseInterface = db;
        }

        public void Alias(CommandInput info) {
            string args = info.Arguments.Trim();
            var argParts = args.Split(' ');

            if (argParts.Length == 3)
                PerformAliasOperation(argParts, info.Source);
            else if (argParts.Length == 1)
                DisplayUserAliases(argParts[0], info.Source, args);
            else
                FattyBot.SendMessage(info.Source, "Format is .alias [add|remove] <existing_nick> <new_alias> to add or remove a nick, or just .alias <nick> to view aliases for that user");
        }

        private void DisplayUserAliases(string alias, string source, string args) {
            StringBuilder sb = new StringBuilder();
            var results = DatabaseInterface.GetAliases(alias);
            string prepend = "Results for " + alias + ": ";
            int prependLength = prepend.Length;
            foreach(string res in results)
            {
                bool success = FattyBot.TryAppend(sb, res + ", ", source);
                if (!success) {
                    sb.Remove(sb.Length - 1, 1);
                    break;
                }
            }
            if (results.Count > 0) {
                FattyBot.SendMessage(source, sb.ToString());
            }
            else {
                FattyBot.SendMessage(source, String.Format("No results found for {0}", args));
            }
        }

        private void PerformAliasOperation(string[] argParts, string source) {
            if (argParts.Length < 3)
                FattyBot.SendMessage(source, "error parsing arguments");
            string operation = argParts[0];
            string firstName = argParts[1];
            string secondName = argParts[2];
            switch (operation) {
                case "add":
                    FattyBot.SendMessage(source, FattyUserAliases.AddAlias(firstName, secondName));
                    break;
                case "remove":
                    FattyBot.SendMessage(source, FattyUserAliases.RemoveAlias(firstName, secondName));
                    break;
                default:
                    FattyBot.SendMessage(source, String.Format("{0} is an unknown operation, try 'add' or 'remove'", operation));
                    break;
            }
        }

    }
    
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

            public string RemoveAlias(string aliasToRemove, string aliasGroupName) {
                UserAliasGroup ag;
                if (!AliasRegistry.TryGetValue(aliasGroupName, out ag))
                    return String.Format("Could not find group containing {0}", aliasGroupName);

                if (ag.RemoveAlias(aliasToRemove))
                    return String.Format("{0} was removed from {1}'s alias group", aliasToRemove, aliasGroupName);

                else
                    return "Something bad has happened";
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

            public bool RemoveAlias(string aliasToRemove) {
                return Aliases.Remove(aliasToRemove);
            }

            public IEnumerable<string> GetUserAliases() {
                return Aliases.AsEnumerable();
            }
        }
}
