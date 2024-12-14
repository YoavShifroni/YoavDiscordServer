using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public static class ChatRoomsManager
    {
        public static void SendMessageThatTheUserSentToTheOtherUsers(int userId, string username, string message, DateTime time)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Message_From_Other_User_Command;
            clientServerProtocol.Username = username;
            clientServerProtocol.UserId = userId;
            clientServerProtocol.MessageThatTheUserSent = message;
            clientServerProtocol.TimeThatTheMessageWasSent = time;
            DiscordClientConnection.SendMessageToAllUserExceptOne(userId , clientServerProtocol);
        }
    }
}
