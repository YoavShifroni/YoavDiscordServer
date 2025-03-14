﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void SendMessageThatTheUserSentToTheOtherUsers(int userId, string username, string message, int chatRoomId)
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
            DiscordClientConnection.SendMessageToAllUserExceptOne(userId , clientServerProtocol);
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


        private static Dictionary<string, Tuple<int, int, string>> GetConnectedUsersDetails(int userId, MediaRoom mediaRoom)
        {
            Dictionary<string, Tuple<int, int, string>> ipsToPortUserIdAndUsername = new Dictionary<string, Tuple<int, int, string>>();
            foreach (int user in mediaRoom.UsersInThisRoom.Keys)
            {
                if(user != userId)
                {
                    ipsToPortUserIdAndUsername.Add(DiscordClientConnection.GetUserIpById(user), Tuple.Create(mediaRoom.UsersInThisRoom[user],
                        user, DiscordClientConnection.GetDiscordClientConnectionById(user).CommandHandlerForSingleUser.Username));
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
    }
}
