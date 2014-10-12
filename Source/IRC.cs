using System;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace FattyBot {
    #region Delegates
    public delegate void ChannelMessage(string ircUser,string ircChannel, string message);
    public delegate void PrivateMessage(string ircUser, string message);
    public delegate void CommandReceived(string ircCommand);
    public delegate void TopicSet(string ircChannel, string ircTopic);
    public delegate void TopicOwner(string ircChannel, string ircUser, string topicDate);
    public delegate void NamesList(string userNames);
    public delegate void ServerMessage(string serverMessage);
    public delegate void Join(string ircChannel, string ircUser);
    public delegate void Part(string ircChannel, string ircUser);
    public delegate void Mode(string ircChannel, string ircUser, string userMode);
    public delegate void NickChange(string UserOldNick, string UserNewNick);
    public delegate void Kick(string ircChannel, string userKicker, string userKicked, string kickMessage);
    public delegate void Quit(string userQuit, string quitMessage);
    public delegate void Notice(string ircUser, string message);
    #endregion

    public class IRC {
        #region Events
        public event ChannelMessage eventChannelMessage;
        public event PrivateMessage eventPrivateMessage;
        public event CommandReceived eventReceiving;
        public event TopicSet eventTopicSet;
        public event TopicOwner eventTopicOwner;
        public event NamesList eventNamesList;
        public event ServerMessage eventServerMessage;
        public event Join eventJoin;
        public event Part eventPart;
        public event Mode eventMode;
        public event NickChange eventNickChange;
        public event Kick eventKick;
        public event Quit eventQuit;
        public event Notice eventNotice;
        #endregion

        #region Private Variables
        private List<string> ChannelList;
        private Object SendMessageLock = new Object();
        #endregion

        #region Properties
        public string IrcServer { get; private set; }
        public int IrcPort { get; private set; }
        public string IrcNick { get; private set; }
        public string IrcUserName { get; private set; }
        public string IrcRealName { get; private set; }
        public string AuthPassword { get; private set; }
        public bool IsInvisble { get; private set; }

        public TcpClient IrcConnection { get; private set; }
        public NetworkStream IrcStream { get; private set; }
        public StreamWriter IrcWriter { get; private set; }
        public StreamReader IrcReader { get; private set; }
        #endregion

        #region Constructor
        public IRC(string ircNick, string ircServer, string[] ircChannels, int ircPort, string authPassword) {
            this.IrcNick = ircNick;
            this.IrcUserName = ircNick;
            this.IrcRealName = "FattyBot v1.0";
            this.IsInvisble = false;
            this.IrcServer = ircServer;
            this.IrcPort = ircPort;
            this.AuthPassword = authPassword;

            this.ChannelList = new List<string>();
            this.ChannelList.AddRange(ircChannels);
        }
        #endregion

        #region Public Methods
        public void Connect() {
            InternalConnect();

            while (true) {
                ListenForCommands();
            }
        }

        public void SendServerMessage(string message) {
            lock (SendMessageLock) {
                this.IrcWriter.WriteLine(message);
                this.IrcWriter.Flush();
            }
        }

        public void JoinChannel(string channelName) {
            this.ChannelList.Add(channelName);
            SendServerMessage(String.Format("JOIN {0}", channelName));
        }

        public void LeaveChannel(string channelName) {
            this.ChannelList.Remove(channelName);
            SendServerMessage(String.Format("PART {0}", channelName));
        }
        #endregion

        #region Private Methods
        #region Server Messages
        private void IrcTopic(string[] ircCommand) {
            string ircChannel = ircCommand[3];
            string ircTopic = "";
            ircTopic = RecombineMessage(ircCommand, 4);
            if (eventTopicSet != null) { this.eventTopicSet(ircChannel, ircTopic.Remove(0, 1).Trim()); }
        }

        private void IrcTopicOwner(string[] ircCommand) {
            string ircChannel = ircCommand[3];
            string ircUser = ircCommand[4].Split('!')[0];
            string topicDate = ircCommand[5];
            if (eventTopicOwner != null) { this.eventTopicOwner(ircChannel, ircUser, topicDate); }
        }

        private void IrcNamesList(string[] ircCommand) {
            string userNames = "";
            userNames = RecombineMessage(ircCommand, 5);
            if (eventNamesList != null) { this.eventNamesList(userNames.Remove(0, 1).Trim()); }
        }

        private void IrcServerMessage(string[] ircCommand) {
            string serverMessage = "";
            serverMessage = RecombineMessage(ircCommand, 1);
            if (eventServerMessage != null) { this.eventServerMessage(serverMessage.Trim()); }
        }

        private void IrcPing(string[] ircCommand) {
            string pingHash = "";
            pingHash = String.Join(" ", ircCommand, 1, ircCommand.Length - 1);
            SendServerMessage("PONG " + pingHash);
        }
        #endregion

        #region User Messages
        private void IrcJoin(string[] ircCommand) {
            string ircChannel = ircCommand[2];
            string ircUser = ircCommand[0].Split('!')[0];
            if (eventJoin != null) { this.eventJoin(ircChannel.Remove(0, 1), ircUser); }
        }

        private void IrcPart(string[] ircCommand) {
            string ircChannel = ircCommand[2];
            string ircUser = ircCommand[0].Split('!')[0];
            if (eventPart != null) { this.eventPart(ircChannel, ircUser); }
        }

        private void IrcMode(string[] ircCommand) {
            string ircChannel = ircCommand[2];
            string ircUser = ircCommand[0].Split('!')[0];
            string userMode = "";
            userMode = RecombineMessage(ircCommand, 3);
            if (userMode.Substring(0, 1) == ":") {
                userMode = userMode.Remove(0, 1);
            }
            if (eventMode != null) { this.eventMode(ircChannel, ircUser, userMode.Trim()); }
        }

        private void IrcNickChange(string[] ircCommand) {
            string userOldNick = ircCommand[0].Split('!')[0];
            string userNewNick = ircCommand[2].Remove(0, 1);
            if (eventNickChange != null) { this.eventNickChange(userOldNick, userNewNick); }
        }

        private void IrcKick(string[] ircCommand) {
            string userKicker = ircCommand[0].Split('!')[0];
            string userKicked = ircCommand[3];
            string ircChannel = ircCommand[2];
            string kickMessage = "";

            kickMessage = RecombineMessage(ircCommand, 4);
            if (eventKick != null) { this.eventKick(ircChannel, userKicker, userKicked, kickMessage.Remove(0, 1).Trim()); }
        }

        private void IrcQuit(string[] ircCommand) {
            string userQuit = ircCommand[0].Split('!')[0];
            string quitMessage = "";

            quitMessage = RecombineMessage(ircCommand, 2);
            if (eventQuit != null) { this.eventQuit(userQuit, quitMessage.Remove(0, 1).Trim()); }
        }

        void IrcPrivateMessage(string[] ircCommand) {
            string userSender = ircCommand[0].Split('!')[0];
            string messageTo = ircCommand[2];
            string message = "";
            message = RecombineMessage(ircCommand, 3);
            message = message.Remove(0, 1).Trim();
            if (messageTo[0] == '#') {
                if (eventChannelMessage != null) { this.eventChannelMessage(userSender, messageTo, message); }
            }
            else if (messageTo == this.IrcNick) {
                if (eventPrivateMessage != null) { this.eventPrivateMessage(userSender, message); }
            }

        }

        void IrcNotice(string[] ircCommand) {
            string UserSender = ircCommand[0].Split('!')[0];
            string message = "";
            message = RecombineMessage(ircCommand, 3);
            message = message.Remove(0, 1).Trim();
            if (eventNotice != null) { this.eventNotice(UserSender, message); }
        }

        #endregion

        #region Utility Methods

        private void ListenForCommands() {
            try {
                string ircCommand;
                while (true) {
                    while ((ircCommand = this.IrcReader.ReadLine()) != null) {

                        Thread th = new Thread(new ThreadStart(() => {
                            if (eventReceiving != null) { this.eventReceiving(ircCommand); }

                            string[] commandParts = ircCommand.Split(' ');
                            if (commandParts[0][0] == ':')
                                commandParts[0] = commandParts[0].Remove(0, 1);

                            if (IsServerMessage(commandParts))
                                HandleServerMessage(commandParts);

                            else if (commandParts[0] == "PING")
                                this.IrcPing(commandParts);
                            else
                                HandleChatMessage(commandParts);
                        }));
                        th.Start();
                        
                    }
                    InternalConnect();
                }
            }
            catch (IOException ex) {
                Console.WriteLine(ex);
                InternalConnect();
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
                InternalConnect();
            }
        }

        private void InternalConnect() {
            // Connect with the IRC server.

            this.IrcConnection = new TcpClient(this.IrcServer, this.IrcPort);
            this.IrcConnection.ReceiveTimeout = 1000 * 60 * 5;
            this.IrcStream = this.IrcConnection.GetStream();
            this.IrcReader = new StreamReader(this.IrcStream);
            this.IrcWriter = new StreamWriter(this.IrcStream);

            Console.WriteLine("Connected to {0}", this.IrcServer);


            // Authenticate our user
            string isInvisible = this.IsInvisble ? "8" : "0";
            Console.WriteLine("Sending user info...");
            SendServerMessage(String.Format("USER {0} {1} * :{2}", this.IrcUserName, isInvisible, this.IrcRealName));          
            SendServerMessage(String.Format("NICK {0}", this.IrcNick));
            
            Thread.Sleep(1000);
            Console.WriteLine("Identifying...");
            SendServerMessage("PRIVMSG NickServ :IDENTIFY " + this.AuthPassword);
            
            Thread.Sleep(5000);
            // wait to be granted our cloak/hostmask
            JoinChannels();                 
        }

        private void JoinChannels() {
            foreach (string chan in this.ChannelList) {
                Thread.Sleep(500);
                Console.WriteLine("Joining " + chan + "...");
                SendServerMessage(String.Format("JOIN {0}", chan));             
            }
        }

        private void HandleServerMessage(string[] commandParts) {
            switch (commandParts[1]) {
                case "332": this.IrcTopic(commandParts); break;
                case "333": this.IrcTopicOwner(commandParts); break;
                case "353": this.IrcNamesList(commandParts); break;
                case "366": /*this.IrcEndNamesList(commandParts);*/ break;
                case "372": /*this.IrcMOTD(commandParts);*/ break;
                case "376": /*this.IrcEndMOTD(commandParts);*/ break;
                case "433":
                    // this is the message we get when nick is already taken
                    Random rand = new Random();
                    //this.IrcNick += rand.Next(9999);
                    SendServerMessage(String.Format("NICK {0}", this.IrcNick+rand.Next(9999)));
                    SendServerMessage(String.Format("PRIVMSG NickServ :ghost {0} {1}", this.IrcNick, this.AuthPassword));
                    Thread.Sleep(1000);
                    SendServerMessage(String.Format("NICK {0}", this.IrcNick));
                    SendServerMessage("PRIVMSG NickServ :IDENTIFY " + this.AuthPassword);
                    JoinChannels();
                    break;
                default: this.IrcServerMessage(commandParts); break;
            }
        }

        private void HandleChatMessage(string[] commandParts) {
            string commandAction = commandParts[1];
            switch (commandAction) {
                case "JOIN": this.IrcJoin(commandParts); break;
                case "PART": this.IrcPart(commandParts); break;
                case "MODE": this.IrcMode(commandParts); break;
                case "NICK": this.IrcNickChange(commandParts); break;
                case "KICK": this.IrcKick(commandParts); break;
                case "QUIT": this.IrcQuit(commandParts); break;
                case "PRIVMSG": this.IrcPrivateMessage(commandParts); break;
                case "NOTICE": this.IrcNotice(commandParts); break;
                //todo: implement topic changed
            }
        }

        private bool IsServerMessage(string[] commandParts) {
            string possibleSender = commandParts[0];
            Regex rgx = new Regex(@"\..*\..+$");
            Match possibleSenderMatch = rgx.Match(possibleSender);
            Match serverNameMatch = rgx.Match(this.IrcServer);
            if (!serverNameMatch.Success)
                throw new Exception("cannot get toplevel domain from provided servername");
            if (!possibleSenderMatch.Success)
                return false;
            string possibleVal = possibleSenderMatch.Value;
            string serverVal = serverNameMatch.Value;
            if (serverVal == possibleVal)
                return true;

            return false;
        }

        string RecombineMessage(string[] messageParts, int startPos) {
            return String.Join(" ", messageParts, startPos, messageParts.Length - startPos);
        }

        #endregion
        #endregion
    }
}