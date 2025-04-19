using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    /// <summary>
    /// Represents a voice/video channel on the server side of the Discord clone application.
    /// Manages the collection of users currently connected to this media room.
    /// </summary>
    /// <remarks>
    /// This server-side class tracks which users are in which media rooms,
    /// and maintains their connection details (particularly their media ports).
    /// It provides methods for adding and removing users from the room.
    /// </remarks>
    public class MediaRoom
    {
        /// <summary>
        /// The unique identifier for this media room.
        /// Corresponds to voice channel IDs in the application.
        /// </summary>
        public int RoomId;

        /// <summary>
        /// Dictionary mapping user IDs to their media ports.
        /// Key: User ID
        /// Value: UDP port where the user is listening for media connections
        /// </summary>
        /// <remarks>
        /// This dictionary serves as both a list of users in the room and 
        /// a lookup table for their media connection details. The server uses
        /// this information to inform other clients about peer connection details.
        /// </remarks>
        public Dictionary<int, int> UsersInThisRoom = new Dictionary<int, int>();

        /// <summary>
        /// Creates a new MediaRoom with the specified room ID.
        /// </summary>
        /// <param name="roomId">The unique identifier for this media room.</param>
        public MediaRoom(int roomId)
        {
            this.RoomId = roomId;
        }

        /// <summary>
        /// Adds a user to this media room.
        /// </summary>
        /// <param name="userId">The ID of the user to add.</param>
        /// <param name="mediaPort">The UDP port where the user is listening for media connections.</param>
        /// <remarks>
        /// If the user is already in the room, this method does nothing.
        /// This prevents duplicate entries and potential connection issues.
        /// </remarks>
        public void AddUser(int userId, int mediaPort)
        {
            if (!this.UsersInThisRoom.ContainsKey(userId))
            {
                this.UsersInThisRoom.Add(userId, mediaPort);
            }
        }

        /// <summary>
        /// Removes a user from this media room.
        /// </summary>
        /// <param name="userId">The ID of the user to remove.</param>
        /// <remarks>
        /// If the user is not in the room, this method does nothing.
        /// The method safely checks if the user exists before attempting removal.
        /// </remarks>
        public void RemoveUser(int userId)
        {
            if (this.UsersInThisRoom.ContainsKey(userId))
            {
                this.UsersInThisRoom.Remove(userId);
            }
        }
    }
}