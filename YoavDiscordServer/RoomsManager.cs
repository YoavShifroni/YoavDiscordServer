using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    /// <summary>
    /// Manages media rooms and chat functionality for the Discord server clone.
    /// Handles user messages, media connections, and room state management.
    /// </summary>
    public static class RoomsManager
    {
        /// <summary>
        /// List of all media rooms available in the server.
        /// Each MediaRoom represents a voice/video channel where users can connect.
        /// </summary>
        public static List<MediaRoom> MediaRooms = new List<MediaRoom>
        {
            new MediaRoom(1),
            new MediaRoom(2),
            new MediaRoom(3)
        };

        /// <summary>
        /// Dictionary storing the mute state of each user.
        /// Key: User ID, Value: Boolean indicating if the user is muted (true) or not (false).
        /// </summary>
        private static Dictionary<int, bool> _userMuteStates = new Dictionary<int, bool>();

        /// <summary>
        /// Dictionary storing the deafen state of each user.
        /// Key: User ID, Value: Boolean indicating if the user is deafened (true) or not (false).
        /// </summary>
        private static Dictionary<int, bool> _userDeafenStates = new Dictionary<int, bool>();

        /// <summary>
        /// Dictionary storing the video mute state of each user.
        /// Key: User ID, Value: Boolean indicating if the user's video is muted (true) or not (false).
        /// </summary>
        private static Dictionary<int, bool> _userVideoMuteStates = new Dictionary<int, bool>();

        /// <summary>
        /// Processes a message sent by a user in a chat room.
        /// First attempts to process the message with the bot manager (for commands),
        /// then broadcasts it to other users if it's not a bot command.
        /// </summary>
        /// <param name="userId">The ID of the user sending the message</param>
        /// <param name="username">The username of the sender</param>
        /// <param name="message">The content of the message</param>
        /// <param name="chatRoomId">The ID of the chat room where the message was sent</param>
        public static async void HandleMessageSentFromUserInChat(int userId, string username, string message, int chatRoomId)
        {
            bool isDone = await BotManager.GetInstance().ProcessMessage(userId, username, message, chatRoomId);
            if (!isDone)
            {
                //Send the message to all other users and store it in the MongoDB
                StoreAndSendMessageToAllUsersButOne(userId, username, message, chatRoomId);
            }
        }

        /// <summary>
        /// Stores a message in the database and broadcasts it to all users except the sender.
        /// </summary>
        /// <param name="userId">The ID of the user sending the message</param>
        /// <param name="username">The username of the sender</param>
        /// <param name="message">The content of the message</param>
        /// <param name="chatRoomId">The ID of the chat room where the message was sent</param>
        public static void StoreAndSendMessageToAllUsersButOne(int userId, string username, string message, int chatRoomId)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Message_From_Other_User_Command;
            clientServerProtocol.Username = username;
            clientServerProtocol.UserId = userId;
            clientServerProtocol.MessageThatTheUserSent = message;
            clientServerProtocol.TimeThatTheMessageWasSent = DateTime.UtcNow;
            clientServerProtocol.ChatRoomId = chatRoomId;
            var userMessage = new UserMessage
            {
                userId = userId,
                Username = username,
                Message = message,
                //Time = DateTime.UtcNow,  // Use current UTC time
                ChatRoomId = chatRoomId
            };
            //MongoDBClient.GetInstance().InsertMessage(userMessage);
            MongoDBRestClient mongoDBRestClient = new MongoDBRestClient();
            mongoDBRestClient.InsertMessage(userMessage);
            DiscordClientConnection.SendMessageToAllUserExceptOne(userId, clientServerProtocol);
        }

        /// <summary>
        /// Retrieves the message history of a chat room and sends it to a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the history</param>
        /// <param name="chatRoomId">The ID of the chat room to retrieve messages from</param>
        public static async void GetMessagesHistoryOfChatRoom(int userId, int chatRoomId)
        {
            //List<UserMessage> messages = await MongoDBClient.GetInstance().GetAllMessageOfChatRoom(chatRoomId);
            MongoDBRestClient mongoDBRestClient = new MongoDBRestClient();
            List<UserMessage> messages = null;
            try
            {
                messages = await mongoDBRestClient.GetAllMessageOfChatRoom(chatRoomId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Return_Messages_History_Of_Chat_Room_Command;
            clientServerProtocol.MessagesOfAChatRoom = messages;
            DiscordClientConnection.SendMessageToSpecificUser(userId, clientServerProtocol);
        }

        /// <summary>
        /// Finds and returns a media room by its ID.
        /// </summary>
        /// <param name="mediaRoomId">The ID of the media room to find</param>
        /// <returns>The MediaRoom object if found, null otherwise</returns>
        public static MediaRoom GetMediaRoomById(int mediaRoomId)
        {
            foreach (MediaRoom media in MediaRooms)
            {
                if (media.RoomId == mediaRoomId)
                {
                    return media;
                }
            }
            return null;
        }

        /// <summary>
        /// Notifies all users in a media room that a new participant has joined.
        /// </summary>
        /// <param name="userId">The ID of the new participant</param>
        /// <param name="username">The username of the new participant</param>
        /// <param name="mediaRoom">The media room that the user joined</param>
        public static void UpdateOthersWhenNewParticipantJoinTheMediaRoom(int userId, string username, MediaRoom mediaRoom)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.New_Participant_Join_The_Media_Room_Command;
            clientServerProtocol.NewParticipantIp = DiscordClientConnection.GetUserIpById(userId);
            clientServerProtocol.MediaPort = mediaRoom.UsersInThisRoom[userId];
            clientServerProtocol.UserId = userId;
            clientServerProtocol.Username = username;
            Console.WriteLine("Message sent to clients: " + clientServerProtocol.ToString());
            foreach (int user in mediaRoom.UsersInThisRoom.Keys)
            {
                if (user != userId)
                {
                    DiscordClientConnection userInTheMediaRoom = DiscordClientConnection.GetDiscordClientConnectionById(user);
                    userInTheMediaRoom.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
                }
            }
        }

        /// <summary>
        /// Provides a new user with information about all other users already in a media room.
        /// </summary>
        /// <param name="userId">The ID of the new user</param>
        /// <param name="mediaRoom">The media room the user joined</param>
        public static void UpdateNewUserAboutTheCurrentUsersInTheMediaRoom(int userId, MediaRoom mediaRoom)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Get_All_Ips_Of_Connected_Users_In_Some_Media_Room_Command;
            clientServerProtocol.UsersMediaConnectionDetails = GetConnectedUsersDetails(userId, mediaRoom);
            DiscordClientConnection newUser = DiscordClientConnection.GetDiscordClientConnectionById(userId);
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            newUser.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
        }

        /// <summary>
        /// Gathers details of all users in a media room except for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user to exclude from the list</param>
        /// <param name="mediaRoom">The media room to get user details from</param>
        /// <returns>A list of UserMediaConnectionDetails for all users in the room except userId</returns>
        private static List<UserMediaConnectionDetails> GetConnectedUsersDetails(int userId, MediaRoom mediaRoom)
        {
            List<UserMediaConnectionDetails> ipsToPortUserIdAndUsername = new List<UserMediaConnectionDetails>();
            foreach (int user in mediaRoom.UsersInThisRoom.Keys)
            {
                if (user != userId)
                {
                    ipsToPortUserIdAndUsername.Add(new UserMediaConnectionDetails(DiscordClientConnection.GetUserIpById(user), mediaRoom.UsersInThisRoom[user],
                        user, DiscordClientConnection.GetDiscordClientConnectionById(user).CommandHandlerForSingleUser.Username, IsUserMuted(user), IsUserDeafened(user), IsUserVideoMuted(user)));
                }
            }
            return ipsToPortUserIdAndUsername;
        }

        /// <summary>
        /// Notifies all users in a media room that a user has left.
        /// </summary>
        /// <param name="userId">The ID of the user who left</param>
        /// <param name="mediaRoom">The media room that the user left</param>
        public static void UpdateEveryoneTheSomeUserLeft(int userId, MediaRoom mediaRoom)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Some_User_Left_The_Media_Room_Command;
            clientServerProtocol.UserIp = DiscordClientConnection.GetUserIpById(userId);
            Console.WriteLine("Message sent to clients: " + clientServerProtocol.ToString());
            foreach (int user in mediaRoom.UsersInThisRoom.Keys)
            {
                if (user != userId)
                {
                    DiscordClientConnection userInTheMediaRoom = DiscordClientConnection.GetDiscordClientConnectionById(user);
                    userInTheMediaRoom.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
                }
            }
            mediaRoom.RemoveUser(userId);
        }

        /// <summary>
        /// Finds which media room a user is currently in.
        /// </summary>
        /// <param name="userId">The ID of the user to locate</param>
        /// <returns>The room ID if the user is in a media room, -1 otherwise</returns>
        public static int GetMediaRoomIdForUser(int userId)
        {
            foreach (MediaRoom room in MediaRooms)
            {
                if (room.UsersInThisRoom.ContainsKey(userId))
                {
                    return room.RoomId;
                }
            }
            return -1;
        }

        /// <summary>
        /// Checks if a user is currently muted.
        /// </summary>
        /// <param name="userId">The ID of the user to check</param>
        /// <returns>True if the user is muted, false otherwise</returns>
        public static bool IsUserMuted(int userId)
        {
            return _userMuteStates.ContainsKey(userId) && _userMuteStates[userId];
        }

        /// <summary>
        /// Checks if a user is currently deafened.
        /// </summary>
        /// <param name="userId">The ID of the user to check</param>
        /// <returns>True if the user is deafened, false otherwise</returns>
        public static bool IsUserDeafened(int userId)
        {
            return _userDeafenStates.ContainsKey(userId) && _userDeafenStates[userId];
        }

        /// <summary>
        /// Checks if a user's video is currently muted.
        /// </summary>
        /// <param name="userId">The ID of the user to check</param>
        /// <returns>True if the user's video is muted, false otherwise</returns>
        public static bool IsUserVideoMuted(int userId)
        {
            return _userVideoMuteStates.ContainsKey(userId) && _userVideoMuteStates[userId];
        }

        /// <summary>
        /// Sets the mute state for a user.
        /// </summary>
        /// <param name="userId">The ID of the user to update</param>
        /// <param name="isMuted">The new mute state (true for muted, false for unmuted)</param>
        public static void SetUserMuted(int userId, bool isMuted)
        {
            _userMuteStates[userId] = isMuted;
        }

        /// <summary>
        /// Sets the deafen state for a user.
        /// </summary>
        /// <param name="userId">The ID of the user to update</param>
        /// <param name="isDeafened">The new deafen state (true for deafened, false for undeafened)</param>
        public static void SetUserDeafened(int userId, bool isDeafened)
        {
            _userDeafenStates[userId] = isDeafened;
        }

        /// <summary>
        /// Sets the video mute state for a user.
        /// </summary>
        /// <param name="userId">The ID of the user to update</param>
        /// <param name="isVideoMuted">The new video mute state (true for video muted, false for video unmuted)</param>
        public static void SetUserVideoMuted(int userId, bool isVideoMuted)
        {
            _userVideoMuteStates[userId] = isVideoMuted;
        }
    }
}