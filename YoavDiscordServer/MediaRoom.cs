using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public class MediaRoom
    {
        public int RoomId;

        public Dictionary< int, int> UsersInThisRoom = new Dictionary< int, int>();

        public MediaRoom(int roomId) 
        {
            this.RoomId = roomId;
        }



        public void AddUser(int userId, int mediaPort)
        {
            if (!this.UsersInThisRoom.ContainsKey(userId))
            {
                this.UsersInThisRoom.Add(userId, mediaPort);
            }
        }

        public void RemoveUser(int userId)
        {
            if (this.UsersInThisRoom.ContainsKey(userId))
            {
                this.UsersInThisRoom.Remove(userId);
            }
        }


    }
}
