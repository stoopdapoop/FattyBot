using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyBot {
    class TellManager {

        private DatabaseManager DatabaseInterface;

        public TellManager(DatabaseManager db) {
            DatabaseInterface = db;
        }

        public bool GetTellsForUser(string name, out List<Tuple<String, DateTime, String>> tells) {
            return DatabaseInterface.GetTells(name, out tells);
        }

        public void AddTellForUser(string to, string from, string message) {

            DatabaseInterface.SetTell(from, message, to);
        }

        public bool CheckTellsForUser(string name) {
            return DatabaseInterface.CheckForTells(name);
        }
    }
}
