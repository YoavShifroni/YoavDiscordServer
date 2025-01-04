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

        public List<int> _usersInThisRoom = new List<int>();

        public MediaRoom(int roomId) 
        {
            this.RoomId = roomId;
        }

       

        //public void AddUser(int userId)
        //{
        //    if (!this._usersInThisRoom.Contains(userId))
        //    {
        //        this._usersInThisRoom.Add(userId);
        //    }
        //}

        //public void RemoveUser(int userId)
        //{
        //    if (this._usersInThisRoom.Contains(userId))
        //    {
        //        this._usersInThisRoom.Remove(userId);
        //    }
        //}

        
    }
}
