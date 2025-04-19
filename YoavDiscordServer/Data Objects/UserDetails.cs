using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    /// <summary>
    /// Represents information about a user in the Discord clone application.
    /// Contains core user data such as identity, appearance, and status.
    /// </summary>
    /// <remarks>
    /// This class is serializable to support network transmission and storage.
    /// It includes information needed to display user entries in the UI and
    /// manage user-related functionality throughout the application.
    /// </remarks>
    [Serializable]
    public class UserDetails
    {
        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public int UserId;

        /// <summary>
        /// The display name of the user.
        /// </summary>
        public string Username;

        /// <summary>
        /// The user's profile picture as a byte array.
        /// </summary>
        public byte[] Picture;

        /// <summary>
        /// The ID of the media channel the user is currently connected to.
        /// A value of -1 indicates the user is not in any media channel.
        /// </summary>
        public int MediaChannelId = -1;

        /// <summary>
        /// The user's role level (e.g., 0 for Admin, 1 for Moderator, 2 for regular Member).
        /// Determines permissions and display characteristics in the UI.
        /// </summary>
        public int role;

        /// <summary>
        /// The user's online status.
        /// True indicates the user is online, false indicates offline.
        /// </summary>
        public bool Status;

        /// <summary>
        /// Default constructor for serialization support.
        /// </summary>
        public UserDetails()
        {
        }

        /// <summary>
        /// Creates a new UserDetails instance with the specified properties.
        /// </summary>
        /// <param name="userId">The unique identifier for the user.</param>
        /// <param name="username">The display name of the user.</param>
        /// <param name="picture">The user's profile picture as a byte array.</param>
        /// <param name="role">The user's role level (0 for Admin, 1 for Moderator, 2+ for regular members).</param>
        public UserDetails(int userId, string username, byte[] picture, int role)
        {
            UserId = userId;
            Username = username;
            Picture = picture;
            this.role = role;
        }
    }
}