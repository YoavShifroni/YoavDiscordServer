using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    /// <summary>
    /// Represents connection details for a user in a media (voice/video) channel.
    /// Contains network information and media status flags.
    /// </summary>
    /// <remarks>
    /// This class is serializable to support network transmission.
    /// It includes all the information needed to establish a peer-to-peer
    /// media connection with another user and display their current media status.
    /// </remarks>
    [Serializable]
    public class UserMediaConnectionDetails
    {
        /// <summary>
        /// The IP address of the user's client.
        /// Used to establish direct peer-to-peer connections for media streaming.
        /// </summary>
        public string Ip;

        /// <summary>
        /// The port number on which the user's client is listening for media connections.
        /// </summary>
        public int Port;

        /// <summary>
        /// The unique identifier for the user.
        /// </summary>
        public int UserId;

        /// <summary>
        /// The display name of the user.
        /// </summary>
        public string Username;

        /// <summary>
        /// Indicates whether the user's microphone is muted.
        /// When true, the user is not transmitting audio.
        /// </summary>
        public bool IsAudioMuted;

        /// <summary>
        /// Indicates whether the user is deafened (not receiving audio).
        /// When true, the user cannot hear other participants.
        /// </summary>
        public bool IsDeafened;

        /// <summary>
        /// Indicates whether the user's camera is disabled.
        /// When true, the user is not transmitting video.
        /// </summary>
        public bool IsVideoMuted;

        /// <summary>
        /// Default constructor for serialization support.
        /// </summary>
        public UserMediaConnectionDetails()
        {
        }

        /// <summary>
        /// Creates a new UserMediaConnectionDetails instance with the specified properties.
        /// </summary>
        /// <param name="ip">The IP address of the user's client.</param>
        /// <param name="port">The port number on which the user's client is listening.</param>
        /// <param name="userId">The unique identifier for the user.</param>
        /// <param name="username">The display name of the user.</param>
        /// <param name="isAudioMuted">Whether the user's microphone is muted.</param>
        /// <param name="isDeafened">Whether the user is deafened (not receiving audio).</param>
        /// <param name="isVideoMuted">Whether the user's camera is disabled.</param>
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