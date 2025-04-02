using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    [Serializable]

    public class UserMediaConnectionDetails
    {
        public string Ip;

        public int Port;

        public int UserId;

        public string Username;

        public bool IsAudioMuted;

        public bool IsDeafened;

        public bool IsVideoMuted;


        public UserMediaConnectionDetails()
        {

        }
        public UserMediaConnectionDetails(string ip, int port, int userId, string username, bool isAudioMuted, bool isDeafened, bool isVideoMuted)
        {
            Ip = ip;
            Port = port;
            UserId = userId;
            Username = username;
            IsAudioMuted = isAudioMuted;
            IsDeafened = isDeafened;
            IsVideoMuted = isVideoMuted;
        }
    }
}
