using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    [Serializable]
    public class UserDetails
    {
        public int UserId;

        public string Username;

        public byte[] Picture;

        public int MediaChannelId = -1;

        public bool Status;

        public UserDetails() 
        {

        }

        public UserDetails(int userId, string username, byte[] picture)
        {
            UserId = userId;
            Username = username;
            Picture = picture;
        }
    }
}
