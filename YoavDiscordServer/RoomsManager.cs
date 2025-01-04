using Newtonsoft.Json;
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

        public static void UpdateOthersWhenNewParticipantJoinTheMediaRoom(int userId, MediaRoom mediaRoom)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.New_Participant_Join_The_Media_Room_Command;
            clientServerProtocol.NewParticipantIp = DiscordClientConnection.GetUserIpById(userId);
            foreach (int user in mediaRoom._usersInThisRoom)
            {
                if(user != userId)
                {
                    DiscordClientConnection userInTheMediaRoom = DiscordClientConnection.GetDiscordClientConnectionById(user);
                    userInTheMediaRoom.SendMessage(clientServerProtocol.Generate());
                }
               

            }       

        }


    }
}
