using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Meebey.SmartIrc4net;

namespace PikardIrcBot
{
    internal static class Pikard
    {
        #region Declarations

        private static readonly IrcClient FreeNode = new IrcClient();

        private const string server = "chat.freenode.net";
        private const int port = 6667;

        private const string channel = "#vidyadev";

        private static string nickname = "pikard";
        private const string username = "pikard";

        public static bool OpPrivileges;

        public static Dictionary<string, string> BannedHosts = new Dictionary<string, string>();
        public static List<BannedUser> BannedUsers = new List<BannedUser>();

        private static readonly BanCheck BanCheck = new BanCheck { Interval = TimeSpan.FromSeconds(1) };

        #endregion Declarations

        #region Entry Point

        public static void Main(string[] argv)
        {
            //Checks if there are users to be unbanned or not
            CheckForLeftOvers();

            Thread.CurrentThread.Name = "Main";

            //Bot setup stuffs~
            FreeNode.ActiveChannelSyncing = true;
            FreeNode.Encoding = Encoding.UTF8;
            FreeNode.SendDelay = 200;
            FreeNode.AutoRejoin = true;
            FreeNode.AutoReconnect = true;
            FreeNode.AutoRejoinOnKick = true;
            FreeNode.AutoRelogin = true;

            //Bot event handlers assignments
            FreeNode.OnQueryMessage += OnQuery;
            FreeNode.OnError += OnError;
            FreeNode.OnConnecting += OnConnecting;
            FreeNode.OnConnected += OnConnected;
            FreeNode.OnChannelMessage += OnChannelMessage;
            FreeNode.OnJoin += OnChannelJoin;
            FreeNode.OnOp += OnOp;
            FreeNode.OnDeop += OnDeop;
            FreeNode.OnDisconnecting += OnDisconnecting;
            FreeNode.OnDisconnected += OnDisconnect;
            FreeNode.OnNickChange += OnNickChange;

            //Try to connect
            try
            {
                FreeNode.Connect(server, port);
            }
            catch (ConnectionException e)
            {
                Console.WriteLine("Couldn't connect!\nReason: {0}", e.Message);
                Main(argv);
            }

            //Login and join
            try
            {
                FreeNode.Login(nickname, "Pikard Irc Bot", 0, username);
                FreeNode.RfcJoin(channel);
                FreeNode.RfcPrivmsg("nickserv", "identify user pass");
                ThreadPool.QueueUserWorkItem(state => FreeNode.Listen());
                Console.ReadLine();
                FreeNode.Disconnect();
            }
            catch (ConnectionException e)
            {
                Console.Write("Error, Message: {0}", e.Message);
                Console.Write("Excaption: {0}", e.StackTrace);
                Exit();
            }
            catch (Exception e)
            {
                Console.Write("Error, Message: {0}", e.Message);
                Console.Write("Excaption: {0}", e.StackTrace);
                Exit();
            }
        }

        #endregion Entry Point

        #region Event Handlers

        #region Connection & Disconnection

        private static void OnDisconnecting(object sender, EventArgs e)
        {
            Console.WriteLine("Disconnecting");
        }

        private static void OnDisconnect(object sender, EventArgs e)
        {
            Exit();
        }

        private static void OnConnecting(object sender, EventArgs e)
        {
            Console.WriteLine("Connecting to Freenode");
        }

        private static void OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Connected");
        }

        #endregion Connection & Disconnection

        #region Channel

        /// <summary>
        /// When someone says something on the channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnChannelMessage(object sender, IrcEventArgs e)
        {
            //If the someone is an operator
            if (FreeNode.GetChannelUser(channel, e.Data.Nick).IsOp)
            {
                switch (e.Data.MessageArray[0])
                {
                    case "!kick":
                        if (e.Data.MessageArray.Length < 2)
                            FreeNode.RfcKick(channel, e.Data.MessageArray[1]);
                        else
                        {
                            string reason = "";
                            for (int i = 2; i < e.Data.MessageArray.Length; i++)
                                reason += e.Data.MessageArray[i] + " ";

                            reason = reason.TrimEnd();
                            FreeNode.RfcKick(channel, e.Data.MessageArray[1], reason);
                        }

                        break;
                    case "!ban":
                        Ban(e.Data.MessageArray);
                        break;
                    case "!kickban":
                        Kickban(e.Data.MessageArray);
                        break;
                    case "!unban":
                        Unban(e.Data.MessageArray[1]);
                        break;
                }
            }
        }

        private static void OnChannelJoin(object sender, IrcEventArgs e)
        {
            if (e.Data.Nick == nickname)
            {
                Console.WriteLine("Standing by");
            }
        }

        #endregion Channel

        #region Operator

        /// <summary>
        /// When the bot gets operator or not. Might be deprecated in the future
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnOp(object sender, IrcEventArgs e)
        {
            if (e.Data.RawMessageArray[4] == nickname)
            {
                Console.WriteLine("Op status granted by {0}", e.Data.Nick);
                OpPrivileges = true;
                BanCheck.Start();
            }
        }

        private static void OnDeop(object sender, IrcEventArgs e)
        {
            if (e.Data.RawMessageArray[4] == nickname)
            {
                Console.WriteLine("Op status revoked by {0}", e.Data.Nick);
                OpPrivileges = false;
            }
        }

        #endregion Operator

        #region Other

        /// <summary>
        /// When someone PMs the bot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnQuery(object sender, IrcEventArgs e)
        {
            //if sender is op
            if (FreeNode.GetChannelUser(channel, e.Data.Nick).IsOp)
            {
                switch (e.Data.MessageArray[0])
                {
                    case "die":
                        if (e.Data.Nick.Contains("Morgawr")) //you know why
                        {
                            FreeNode.RfcPrivmsg(e.Data.Nick, string.Format("Action disabled for user {0}", e.Data.Nick));
                            break;
                        }
                        Exit();
                        break;
                    case "banned":
                        foreach (BannedUser user in BannedUsers)
                        {
                            string m = string.Format("User {0} at {1} for {2} minutes", user.NickName, user.BanTime,
                                                     user.BanDuration);
                            FreeNode.RfcPrivmsg(e.Data.Nick, m);
                        }
                        break;
                    case "nick":
                        FreeNode.RfcNick(e.Data.MessageArray[1]);
                        break;
                }
            }
        }

        private static void OnError(object sender, ErrorEventArgs e)
        {
            Console.Write("Error: {0}", e.ErrorMessage);
            FreeNode.Disconnect();
            Exit();
        }

        private static void Exit()
        {
            Console.WriteLine("Exiting...");
            Environment.Exit(0);
        }

        /// <summary>
        /// Internally tracks the name of the bot
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnNickChange(object sender, IrcEventArgs e)
        {
            if (e.Data.RawMessageArray[0].Contains(nickname))
            {
                nickname = e.Data.RawMessageArray[2];
                nickname = nickname.Remove(0, 1);
                Console.WriteLine("New nick: {0}", nickname);
            }
        }

        #endregion Other

        #endregion Event Handlers

        #region Helper Methods

        /// <summary>
        /// Bannage
        /// </summary>
        /// <param name="data"></param>
        private static void Ban(string[] data)
        {
            ChannelUser user = FreeNode.GetChannelUser(channel, data[1]);
            int duration;
            bool reason = true;

            //If an invalid username is given
            if (user == null)
            {
                FreeNode.RfcPrivmsg(channel, string.Format("User {0} does not exist in current channel", data[1]));
                return;
            }

            ////safeguard
            //if (user.Nick == "firecrackers")
            //{
            //    FreeNode.RfcPrivmsg(channel, "Cannot comply");
            //    return;
            //}

            //If the given user nickname is the bot (Morgawr)
            if (user.Nick == nickname)
            {
                FreeNode.RfcPrivmsg(channel, "Cannot ban myself");
                return;
            }

            //if the user is already banned
            if (BannedHosts.ContainsKey(user.Nick))
            {
                FreeNode.RfcPrivmsg(channel, string.Format("User {0} is already banned", data[1]));
                return;
            }

            //if no duration given
            if (data.Length < 3)
            {
                FreeNode.RfcPrivmsg(channel, "Insufficient parameters");
                return;
            }

            //if no reason given
            if (data.Length < 4)
            {
                reason = false;
            }

            //if the duration given is invalid (not an integer)
            if (!int.TryParse(data[2], out duration))
            {
                FreeNode.RfcPrivmsg(channel, string.Format("Parameter [{0}] is invalid", data[2]));
                return;
            }

            BannedUser ban = new BannedUser(user.Nick, DateTime.Now, TimeSpan.FromMinutes(duration)) { BanString = user.Host };

            BannedUsers.Add(ban);
            BannedHosts.Add(user.Nick, user.Host);

            string message;
            if (reason)
            {
                string s = "";
                for (int i = 3; i < data.Length; i++)
                    s += data[i] + " ";
                message = string.Format("Banning user {0} for {1} minutes Reason: {2}", user.Nick, duration, s);
            }
            else
            {
                message = string.Format("Banning user {0} for {1} minutes Reason: no reason given", user.Nick, duration);
            }
            FreeNode.RfcPrivmsg(channel, message);
            FreeNode.Ban(channel, user.Host);
            FreeNode.RfcKick(channel, user.Nick);

            SerializeToXml(BannedUsers);
        }

        private static void Kickban(string[] data)
        {
            ChannelUser user = FreeNode.GetChannelUser(channel, data[1]);

            //If an invalid username is given
            if (user == null)
            {
                FreeNode.RfcPrivmsg(channel, string.Format("User {0} does not exist in current channel", data[1]));
                return;
            }

            ////safeguard
            //if (user.Nick == "firecrackers")
            //{
            //    FreeNode.RfcPrivmsg(channel, "Cannot comply");
            //    return;
            //}

            //If the given user nickname is the bot (Morgawr)
            if (user.Nick == nickname)
            {
                FreeNode.RfcPrivmsg(channel, "Cannot ban myself");
                return;
            }

            //if the user is already banned
            if (BannedHosts.ContainsKey(user.Nick))
            {
                FreeNode.RfcPrivmsg(channel, string.Format("User {0} is already banned", data[1]));
                return;
            }
            BannedUser ban = new BannedUser(user.Nick, DateTime.Now, TimeSpan.FromSeconds(10)) { BanString = user.Host };
            BannedUsers.Add(ban);
            BannedHosts.Add(user.Nick, user.Host);

            FreeNode.RfcPrivmsg(channel, string.Format("Kicking user {0} for 10 seconds", user.Nick));
            FreeNode.Ban(channel, user.Host);
            FreeNode.RfcKick(channel, user.Nick);
        }

        private static void CheckForLeftOvers()
        {
            try
            {
                BannedUsers = DeserializeFromXml();
                if (BannedUsers.Count > 0)
                {
                    Console.WriteLine("Found {0} banned users", BannedUsers.Count);
                    foreach (BannedUser user in BannedUsers)
                    {
                        Console.WriteLine("{0} at {1} for {2}.\nBan String {3}", user.NickName, user.BanTime, user.BanDuration, user.BanString);
                        BannedHosts.Add(user.NickName, user.BanString);
                    }
                }
                else
                {
                    Console.WriteLine("No leftovers");
                }
            }
            catch
            {
                BannedUsers = new List<BannedUser>();
                Console.WriteLine("No leftovers");
            }
        }

        #endregion Helper Methods

        #region Public Methods

        public static void Unban(string user)
        {
            FreeNode.Unban(channel, BannedHosts[user]);
            BannedHosts.Remove(user);
            FreeNode.RfcInvite(user, channel);
            FreeNode.RfcPrivmsg(user, string.Format("Your ban on {0} has been revoked, you are allowed to rejoin", channel));
        }

        #endregion Public Methods

        #region Serialization

        /// <summary>
        /// This does Serializing Voodoo and saves the banned users to disk
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void SerializeToXml<T>(List<T> list)
        {
            XmlWriterSettings settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, Encoding = Encoding.ASCII };
            using (XmlWriter writer = XmlWriter.Create("banned.xml", settings))
            {
                XmlRootAttribute rootAtt = new XmlRootAttribute("BannedUsers");

                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");
                XmlSerializer serializer = new XmlSerializer(typeof(List<T>), rootAtt);
                serializer.Serialize(writer, list, namespaces);
                writer.Close();
            }
        }

        /// <summary>
        /// Deserializing voodoo, loads the banned users from disk
        /// </summary>
        /// <returns></returns>
        public static List<BannedUser> DeserializeFromXml()
        {
            using (XmlReader reader = XmlReader.Create("banned.xml"))
            {
                XmlRootAttribute rootAtt = new XmlRootAttribute("BannedUsers");
                XmlSerializer deserializer = new XmlSerializer(typeof(List<BannedUser>), rootAtt);
                List<BannedUser> bannedUsers = (List<BannedUser>)deserializer.Deserialize(reader);
                reader.Close();

                return bannedUsers;
            }
        }

        #endregion Serialization
    }
}