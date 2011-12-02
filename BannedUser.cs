using System;
using System.Xml.Serialization;

namespace PikardIrcBot
{
    public class BannedUser
    {
        public string NickName;

        [XmlIgnore]
        public DateTime BanTime;
        [XmlIgnore]
        public TimeSpan BanDuration;

        [XmlElement("BanStamp")]
        public string BanStamp
        {
            get { return BanTime.ToString(); }
            set { BanTime = DateTime.Parse(value); }
        }

        [XmlElement("BanDuration")]
        public long BanTicks
        {
            get { return BanDuration.Ticks; }
            set { BanDuration = new TimeSpan(value); }
        }

        [XmlElement("BanString")]
        public string BanString { get; set; }

        public BannedUser(string nick, DateTime time, TimeSpan duration)
        {
            NickName = nick;
            BanTime = time;
            BanDuration = duration;
        }

        public BannedUser(string nick, DateTime time)
        {
            NickName = nick;
            BanTime = time;
            BanDuration = new TimeSpan(365, 0, 0, 0);
        }

        public BannedUser()
        {
        }
    }
}