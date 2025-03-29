using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public static class RoomsManager
    {

        public static List<MediaRoom> MediaRooms = new List<MediaRoom>
        {
            new MediaRoom(1),
            new MediaRoom(2),
            new MediaRoom(3)
        };

        private static Dictionary<int, bool> _userMuteStates = new Dictionary<int, bool>();

        private static Dictionary<int, bool> _userDeafenStates = new Dictionary<int, bool>();

        private static Dictionary<int, bool> _userVideoMuteStates = new Dictionary<int, bool>();

        public static async void HandleMessageSentFromUserInChat(int userId, string username, string message, int chatRoomId)
        {
            bool isDone = await BotManager.GetInstance().ProcessMessage(userId, username, message, chatRoomId);
            if (!isDone)
            {
                //Send the message to all other users and store it in the MongoDB
                StoreAndSendMessageToAllUsersButOne(userId, username, message, chatRoomId);
            }
        }

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
                Time = DateTime.UtcNow,  // Use current UTC time
                ChatRoomId = chatRoomId
            };
            MongoDBClient.GetInstance().InsertMessage(userMessage);
            DiscordClientConnection.SendMessageToAllUserExceptOne(userId, clientServerProtocol);
        }

        public static async void GetMessagesHistoryOfChatRoom(int userId, int chatRoomId)
        {
            List<UserMessage> messages = await MongoDBClient.GetInstance().GetAllMessageOfChatRoom(chatRoomId);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Return_Messages_History_Of_Chat_Room_Command;
            clientServerProtocol.MessagesOfAChatRoomJson = JsonConvert.SerializeObject(messages);
            DiscordClientConnection.SendMessageToSpecificUser(userId , clientServerProtocol);
        }

        public static MediaRoom GetMediaRoomById(int mediaRoomId)
        {
            foreach(MediaRoom media in MediaRooms)
            {
                if(media.RoomId == mediaRoomId)
                {
                    return media;
                }
            }
            return null;
        }

        public static void UpdateOthersWhenNewParticipantJoinTheMediaRoom(int userId, string username, MediaRoom mediaRoom)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.New_Participant_Join_The_Media_Room_Command;
            clientServerProtocol.NewParticipantIp = DiscordClientConnection.GetUserIpById(userId);
            clientServerProtocol.MediaPort = mediaRoom.UsersInThisRoom[userId];
            clientServerProtocol.UserId = userId;
            clientServerProtocol.Username = username;
            foreach (int user in mediaRoom.UsersInThisRoom.Keys)
            {
                if(user != userId)
                {
                    DiscordClientConnection userInTheMediaRoom = DiscordClientConnection.GetDiscordClientConnectionById(user);
                    userInTheMediaRoom.SendMessage(clientServerProtocol.Generate());
                }
               

            }      
        }

        public static void UpdateNewUserAboutTheCurrentUsersInTheMediaRoom(int userId, MediaRoom mediaRoom)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Get_All_Ips_Of_Connected_Users_In_Some_Media_Room_Command;
            clientServerProtocol.AllTheConnectedUsersInSomeMediaRoomIpsJson = JsonConvert.SerializeObject(GetConnectedUsersDetails(userId, mediaRoom));
            DiscordClientConnection newUser = DiscordClientConnection.GetDiscordClientConnectionById(userId);
            newUser.SendMessage(clientServerProtocol.Generate());
        }


        private static Dictionary<string, Tuple<int, int, string, bool, bool, bool>> GetConnectedUsersDetails(int userId, MediaRoom mediaRoom)
        {
            Dictionary<string, Tuple<int, int, string, bool, bool, bool>> ipsToPortUserIdAndUsername = new Dictionary<string, Tuple<int, int, string, bool, bool, bool>>();
            foreach (int user in mediaRoom.UsersInThisRoom.Keys)
            {
                if(user != userId)
                {
                    ipsToPortUserIdAndUsername.Add(DiscordClientConnection.GetUserIpById(user), Tuple.Create(mediaRoom.UsersInThisRoom[user],
                        user, DiscordClientConnection.GetDiscordClientConnectionById(user).CommandHandlerForSingleUser.Username, IsUserMuted(user), IsUserDeafened(user), IsUserVideoMuted(user)));
                }
            }
            return ipsToPortUserIdAndUsername;
        }

        

        public static void UpdateEveryoneTheSomeUserLeft(int userId, MediaRoom mediaRoom)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Some_User_Left_The_Media_Room_Command;
            clientServerProtocol.UserIp = DiscordClientConnection.GetUserIpById(userId);
            foreach (int user in mediaRoom.UsersInThisRoom.Keys)
            {
                if (user != userId)
                {
                    DiscordClientConnection userInTheMediaRoom = DiscordClientConnection.GetDiscordClientConnectionById(user);
                    userInTheMediaRoom.SendMessage(clientServerProtocol.Generate());
                }


            }
            mediaRoom.RemoveUser(userId);

        }

        public static int GetMediaRoomIdForUser(int userId)
        {
            foreach(MediaRoom room in MediaRooms)
            {
                if (room.UsersInThisRoom.ContainsKey(userId))
                {
                    return room.RoomId;
                }
            }
            return -1;
        }

        public static bool IsUserMuted(int userId)
        {
            return _userMuteStates.ContainsKey(userId) && _userMuteStates[userId];
        }

        public static bool IsUserDeafened(int userId)
        {
            return _userDeafenStates.ContainsKey(userId) && _userDeafenStates[userId];
        }

        public static bool IsUserVideoMuted(int userId)
        {
            return _userVideoMuteStates.ContainsKey(userId) && _userVideoMuteStates[userId];
        }

        public static void SetUserMuted(int userId, bool isMuted)
        {
            _userMuteStates[userId] = isMuted;
        }

        public static void SetUserDeafened(int userId, bool isDeafened)
        {
            _userDeafenStates[userId] = isDeafened;
        }

        public static void SetUserVideoMuted(int userId, bool isVideoMuted)
        {
            _userVideoMuteStates[userId] = isVideoMuted;
        }
    }
}
