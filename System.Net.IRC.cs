using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text;

namespace System.Net {
	#region Delegates
    public delegate void ChannelMessage(string ircUser, string message);
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
        // aehuaeahu
		#endregion
		
		#region Properties
        public string IrcServer { get; set;  }
        public int IrcPort { get; set; }
        public string IrcNick { get; set; }
        public string IrcUser { get; set; }
        public string IrcRealName { get; set; }
        public string IrcChannel { get; set; } 
        public string AuthPassword { get; set; }
        public bool IsInvisble { get; set; }

        public TcpClient IrcConnection { get; set; }
        public NetworkStream IrcStream { get; set; }
        public StreamWriter IrcWriter { get; set; }
        public StreamReader IrcReader { get; set; } 
		#endregion	
		
		#region Constructor
		public IRC(string IrcNick, string IrcChannel) {
			this.IrcNick = IrcNick;
			this.IrcUser = System.Environment.MachineName;
			this.IrcRealName = "FattyBot v1.0";
			this.IrcChannel = IrcChannel;
			this.IsInvisble = false;
		} /* IRC */
		#endregion
		
		#region Public Methods
		public void Connect(string ircServer, int ircPort, string authPassword) {
			this.IrcServer = ircServer;
			this.IrcPort = ircPort;
            this.AuthPassword = authPassword;

			// Connect with the IRC server.
			this.IrcConnection = new TcpClient(this.IrcServer, this.IrcPort);
			this.IrcStream = this.IrcConnection.GetStream();
			this.IrcReader = new StreamReader(this.IrcStream);
			this.IrcWriter = new StreamWriter(this.IrcStream);

            Console.WriteLine("Connected to {0}", ircServer);


			// Authenticate our user
			string isInvisible = this.IsInvisble ? "8" : "0";
			this.IrcWriter.WriteLine(String.Format("USER {0} {1} * :{2}", this.IrcUser, isInvisible, this.IrcRealName));
			this.IrcWriter.Flush();
			this.IrcWriter.WriteLine(String.Format("NICK {0}", this.IrcNick));
			this.IrcWriter.Flush();
            this.IrcWriter.WriteLine("PRIVMSG NickServ :IDENTIFY " + authPassword);
            this.IrcWriter.Flush();
            Thread.Sleep(400);
			this.IrcWriter.WriteLine(String.Format("JOIN {0}", this.IrcChannel));
			this.IrcWriter.Flush();

			// Listen for commands
			while (true) {
                ListenForCommands();			
			}
		} /* Connect */

        private void ListenForCommands()
        {
            try
            {
                //todo: function this, yo
                if(!this.IrcConnection.Connected)
                {
                    Thread.Sleep(5000);
                    this.IrcConnection = new TcpClient(this.IrcServer, this.IrcPort);
			        this.IrcStream = this.IrcConnection.GetStream();
			        this.IrcReader = new StreamReader(this.IrcStream);
                    this.IrcWriter = new StreamWriter(this.IrcStream);

                    Console.WriteLine("Connected to {0}", IrcServer);

                    // Authenticate our user
                    string isInvisible = this.IsInvisble ? "8" : "0";
                    this.IrcWriter.WriteLine(String.Format("USER {0} {1} * :{2}", this.IrcUser, isInvisible, this.IrcRealName));
                    this.IrcWriter.Flush();
                    this.IrcWriter.WriteLine(String.Format("NICK {0}", this.IrcNick));
                    this.IrcWriter.Flush();
                    this.IrcWriter.WriteLine("PRIVMSG NickServ :IDENTIFY " + AuthPassword);
                    this.IrcWriter.Flush();
                    Thread.Sleep(400);
                    this.IrcWriter.WriteLine(String.Format("JOIN {0}", this.IrcChannel));
                    this.IrcWriter.Flush();
                }

                string ircCommand;
                while ((ircCommand = this.IrcReader.ReadLine()) != null)
                {
                    if (eventReceiving != null) { this.eventReceiving(ircCommand); }

                    string[] commandParts = new string[ircCommand.Split(' ').Length];
                    commandParts = ircCommand.Split(' ');
                    if (commandParts[0].Substring(0, 1) == ":")
                    {
                        commandParts[0] = commandParts[0].Remove(0, 1);
                    }

                    if (commandParts[0] == this.IrcServer)
                    {
                        // Server message
                        switch (commandParts[1])
                        {
                            case "332": this.IrcTopic(commandParts); break;
                            case "333": this.IrcTopicOwner(commandParts); break;
                            case "353": this.IrcNamesList(commandParts); break;
                            case "366": /*this.IrcEndNamesList(commandParts);*/ break;
                            case "372": /*this.IrcMOTD(commandParts);*/ break;
                            case "376": /*this.IrcEndMOTD(commandParts);*/ break;
                            case "433": 
                                Random rand = new Random();
                                this.IrcWriter.WriteLine(String.Format("NICK {0}", this.IrcNick+rand.Next(9999)));
			                    this.IrcWriter.Flush();
                                Thread.Sleep(400);
                                this.IrcWriter.WriteLine(String.Format("JOIN {0}", this.IrcChannel));
			                    this.IrcWriter.Flush();
                                break;
                            default: this.IrcServerMessage(commandParts); break;

                        }
                    }
                    else if (commandParts[0] == "PING")
                    {
                        // Server PING, send PONG back
                        this.IrcPing(commandParts);
                    }
                    else
                    {
                        // Normal message
                        string commandAction = commandParts[1];
                        switch (commandAction)
                        {
                            case "JOIN": this.IrcJoin(commandParts); break;
                            case "PART": this.IrcPart(commandParts); break;
                            case "MODE": this.IrcMode(commandParts); break;
                            case "NICK": this.IrcNickChange(commandParts); break;
                            case "KICK": this.IrcKick(commandParts); break;
                            case "QUIT": this.IrcQuit(commandParts); break;
                            case "PRIVMSG": this.IrcPrivateMessage(commandParts); break;
                            case "NOTICE": this.IrcNotice(commandParts); break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if(this.IrcConnection.Connected)
                    throw;
                
            }
            
        }
		#endregion
		
		#region Private Methods
		#region Server Messages
		private void IrcTopic(string[] ircCommand) {
			string IrcChannel = ircCommand[3];
			string IrcTopic = "";
			for (int intI = 4; intI < ircCommand.Length; intI++) {
				IrcTopic += ircCommand[intI] + " ";
			}
			if (eventTopicSet != null) { this.eventTopicSet(IrcChannel, IrcTopic.Remove(0, 1).Trim()); }
		} /* IrcTopic */
		
		private void IrcTopicOwner(string[] ircCommand) {
			string IrcChannel = ircCommand[3];
			string IrcUser = ircCommand[4].Split('!')[0];
			string TopicDate = ircCommand[5];
			if (eventTopicOwner != null) { this.eventTopicOwner(IrcChannel, IrcUser, TopicDate); }
		} /* IrcTopicOwner */
		
		private void IrcNamesList(string[] ircCommand) {
		  string UserNames = "";
			for (int intI = 5; intI < ircCommand.Length; intI++) {
				UserNames += ircCommand[intI] + " ";
			}
			if (eventNamesList != null) { this.eventNamesList(UserNames.Remove(0, 1).Trim()); }
		} /* IrcNamesList */
		
		private void IrcServerMessage(string[] ircCommand) {
			string ServerMessage = "";
			for (int intI = 1; intI < ircCommand.Length; intI++) {
				ServerMessage += ircCommand[intI] + " ";
			}
			if (eventServerMessage != null) { this.eventServerMessage(ServerMessage.Trim()); }
		} /* IrcServerMessage */
		#endregion
		
		#region Ping
		private void IrcPing(string[] ircCommand) {
			string PingHash = "";
			for (int intI = 1; intI < ircCommand.Length; intI++) {
				PingHash += ircCommand[intI] + " ";
			}
			this.IrcWriter.WriteLine("PONG " + PingHash);
			this.IrcWriter.Flush();
		} /* IrcPing */
		#endregion
		
		#region User Messages
		private void IrcJoin(string[] ircCommand) {
			string IrcChannel = ircCommand[2];
			string IrcUser = ircCommand[0].Split('!')[0];
			if (eventJoin != null) { this.eventJoin(IrcChannel.Remove(0, 1), IrcUser); }
		} /* IrcJoin */
		
		private void IrcPart(string[] ircCommand) {
			string IrcChannel = ircCommand[2];
			string IrcUser = ircCommand[0].Split('!')[0];
			if (eventPart != null) { this.eventPart(IrcChannel, IrcUser); }
		} /* IrcPart */
		
		private void IrcMode(string[] ircCommand) {
			string IrcChannel = ircCommand[2];
			string IrcUser = ircCommand[0].Split('!')[0];
			string UserMode = "";
			for (int intI = 3; intI < ircCommand.Length; intI++) {
				UserMode += ircCommand[intI] + " ";
			}
			if (UserMode.Substring(0, 1) == ":") {
				UserMode = UserMode.Remove(0, 1);
			}
			if (eventMode != null) { this.eventMode(IrcChannel, IrcUser, UserMode.Trim()); }
		} /* IrcMode */
		
		private void IrcNickChange(string[] ircCommand) {
			string UserOldNick = ircCommand[0].Split('!')[0];
			string UserNewNick = ircCommand[2].Remove(0, 1);
			if (eventNickChange != null) { this.eventNickChange(UserOldNick, UserNewNick); }
		} /* IrcNickChange */
		
		private void IrcKick(string[] ircCommand) {
			string UserKicker = ircCommand[0].Split('!')[0];
			string UserKicked = ircCommand[3];
			string IrcChannel = ircCommand[2];
			string KickMessage = "";
			for (int intI = 4; intI < ircCommand.Length; intI++) {
				KickMessage += ircCommand[intI] + " ";
			}
			if (eventKick != null) { this.eventKick(IrcChannel, UserKicker, UserKicked, KickMessage.Remove(0, 1).Trim()); }
		} /* IrcKick */
		
		private void IrcQuit(string[] ircCommand) {
			string UserQuit = ircCommand[0].Split('!')[0];
			string QuitMessage = "";
			for (int intI = 2; intI < ircCommand.Length; intI++) {
				QuitMessage += ircCommand[intI] + " ";
			}
			if (eventQuit != null) { this.eventQuit(UserQuit, QuitMessage.Remove(0, 1).Trim()); }
		} /* IrcQuit */

        void IrcPrivateMessage(string[] ircCommand)
        {
            string UserSender = ircCommand[0].Split('!')[0];
            string MessageTo = ircCommand[2];
            StringBuilder MessageBuilder = new StringBuilder(ircCommand[3].Substring(1));
            for (int intI = 4; intI < ircCommand.Length; intI++) {
                MessageBuilder.Append(" " + ircCommand[intI]);
            }
            if (MessageTo == this.IrcChannel) {
                if (eventChannelMessage != null) { this.eventChannelMessage(UserSender, MessageBuilder.ToString()); }
            }
            else if (MessageTo == this.IrcNick) {
                if (eventPrivateMessage != null) { this.eventPrivateMessage(UserSender, MessageBuilder.ToString()); }
            }         

        }

        void IrcNotice(string[] ircCommand)
        {
            string UserSender = ircCommand[0].Split('!')[0];
            StringBuilder MessageBuilder = new StringBuilder(ircCommand[3].Substring(1));
            for (int intI = 4; intI < ircCommand.Length; intI++) {
                MessageBuilder.Append(" " + ircCommand[intI]);
            }
            if (eventNotice != null) { this.eventNotice(UserSender, MessageBuilder.ToString()) ;}
        }

		#endregion
		#endregion
	} /* IRC */
} /* System.Net */