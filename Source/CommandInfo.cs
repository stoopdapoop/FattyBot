using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyBot {

    public enum SourceType {
        Channel,
        PrivateMessage,
        Notice
    }

    public class CommandInfo {
        public string Caller { get; private set; }
        public string Arguments { get; private set; }
        public string Source { get; private set; }
        public SourceType Origin { get; private set; }
        public string CommandName { get; private set; }
        public CommandInfo(string caller, string args, string source, string commandName, SourceType origin) {
            this.Caller = caller;
            this.Arguments = args;
            this.Source = source;
            this.Origin = origin;
            this.CommandName = commandName;
        }
    }
}
