using System;
using System.Collections.Generic;
using YoavDiscordServer;

namespace YoavDiscordServer
{
    public class ClientServerProtocol
    {
        /// <summary>
        /// The command
        /// </summary>
        public TypeOfCommand TypeOfCommand { get; set; }

        /// <summary>
        /// The username of this user
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password of this user
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The first name of this user
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// The last name of this user
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// The email of this user
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// The city of this user
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// The gender of this user
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// The message that will be showed to the user
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// The code that sent to the user mail
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The profile picture of this user converts to byte's array
        /// </summary>
        public byte[] ProfilePicture { get; set; }

        /// <summary>
        /// The number of minutes that the user need to wait
        /// </summary>
        public int TimeToCooldown { get; set; }

        public string MessageThatTheUserSent { get; set; }

        public DateTime TimeThatTheMessageWasSent { get; set; }

        public int ChatRoomId { get; set; }

        public int MediaRoomId { get; set; }

        public int UserId { get; set; }


        public List<UserMessage> MessagesOfAChatRoom { get; set; }

        public string NewParticipantIp { get; set; }

        public List<UserMediaConnectionDetails> UsersMediaConnectionDetails { get; set; }

        public int MediaPort { get; set; }


        public string UserIp { get; set; }

        public List<UserDetails> AllUsersDetails { get; set; }

        public bool IsMuted { get; set; }

        public bool IsVideoMuted { get; set; }

        public bool IsDeafened { get; set; }

        public int Role { get; set; }


        /// <summary>
        /// Empty constructor
        /// </summary>
        public ClientServerProtocol()
        {

        }



        public ClientServerProtocol(TypeOfCommand typeOfCommand, string username, string password, string firstName, string lastName, string email, string city,
            string gender, string errorMessage, string code, byte[] profilePicture, int timeToCooldown, string messageThatTheUserSent,
            DateTime timeThatTheMessageWasSent, int chatRoomId, int mediaRoomId, int userId, List<UserMessage> messagesOfAChatRoom, string newParticipantIp,
            List<UserMediaConnectionDetails> usersMediaConnectionDetails, int mediaPort, string userIp, List<UserDetails> allUsersDetails, bool isMuted,
            bool isVideoMuted, bool isDeafened, int role)
        {
            TypeOfCommand = typeOfCommand;
            Username = username;
            Password = password;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            City = city;
            Gender = gender;
            ErrorMessage = errorMessage;
            Code = code;
            ProfilePicture = profilePicture;
            TimeToCooldown = timeToCooldown;
            MessageThatTheUserSent = messageThatTheUserSent;
            TimeThatTheMessageWasSent = timeThatTheMessageWasSent;
            ChatRoomId = chatRoomId;
            MediaRoomId = mediaRoomId;
            UserId = userId;
            MessagesOfAChatRoom = messagesOfAChatRoom;
            NewParticipantIp = newParticipantIp;
            UsersMediaConnectionDetails = usersMediaConnectionDetails;
            MediaPort = mediaPort;
            UserIp = userIp;
            AllUsersDetails = allUsersDetails;
            IsMuted = isMuted;
            IsVideoMuted = isVideoMuted;
            IsDeafened = isDeafened;
            Role = role;
        }

        public override string ToString()
        {

            // Add TypeOfCommand (enum, can't be null)
            string toReturn = $"TypeOfCommand: {this.TypeOfCommand}" + Environment.NewLine;

            // Add if not empty or null
            if (!string.IsNullOrEmpty(this.Username))
            {
                toReturn += $"Username: {this.Username}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.Password))
            {
                toReturn += $"Password: {this.Password}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.FirstName))
            {
                toReturn += $"FirstName: {this.FirstName}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.LastName))
            {
                toReturn += $"LastName: {this.LastName}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.Email))
            {
                toReturn += $"Email: {this.Email}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.City))
            {
                toReturn += $"City: {this.City}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.Gender))
            {
                toReturn += $"Gender: {this.Gender}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.ErrorMessage))
            {
                toReturn += $"ErrorMessage: {this.ErrorMessage}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.Code))
            {
                toReturn += $"Code: {this.Code}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.MessageThatTheUserSent))
            {
                toReturn += $"MessageThatTheUserSent: {this.MessageThatTheUserSent}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.NewParticipantIp))
            {
                toReturn += $"NewParticipantIp: {this.NewParticipantIp}" + Environment.NewLine;
            }
            if (!string.IsNullOrEmpty(this.UserIp))
            {
                toReturn += $"UserIp: {this.UserIp}" + Environment.NewLine;
            }

            if (this.ProfilePicture != null && this.ProfilePicture.Length > 0)
            {
                toReturn += $"ProfilePicture: [Byte array of length {this.ProfilePicture.Length}]" + Environment.NewLine;
            }

            if (this.TimeThatTheMessageWasSent != default(DateTime)) // default DateTime object value is 01/01/0001 00:00:00
            {
                toReturn += $"TimeThatTheMessageWasSent: {this.TimeThatTheMessageWasSent}" + Environment.NewLine;
            }

            // Check integer values (only add non-zero values)
            if (this.TimeToCooldown != 0)
            {
                toReturn += $"TimeToCooldown: {this.TimeToCooldown}" + Environment.NewLine;
            }
            if (this.ChatRoomId != 0)
            {
                toReturn += $"ChatRoomId: {this.ChatRoomId}" + Environment.NewLine;
            }
            if (this.MediaRoomId != 0)
            {
                toReturn += $"MediaRoomId: {this.MediaRoomId}" + Environment.NewLine;
            }
            if (this.UserId != 0)
            {
                toReturn += $"UserId: {this.UserId}" + Environment.NewLine;
            }
            if (this.MediaPort != 0)
            {
                toReturn += $"MediaPort: {this.MediaPort}" + Environment.NewLine;
            }
            if (TypeOfCommand == TypeOfCommand.Update_User_Role_Command || TypeOfCommand == TypeOfCommand.Success_Connected_To_The_Application_Command ||
                TypeOfCommand == TypeOfCommand.User_Join_Media_Channel_Command || TypeOfCommand == TypeOfCommand.User_Role_Has_Been_Updated_Command)
            {
                toReturn += $"Role: {this.Role}" + Environment.NewLine;
            }
            if (TypeOfCommand == TypeOfCommand.Set_Mute_User_Command || TypeOfCommand == TypeOfCommand.User_Join_Media_Channel_Command ||
                TypeOfCommand == TypeOfCommand.User_Muted_Command)
            {
                toReturn += $"IsMuted: {this.IsMuted}" + Environment.NewLine;
            }
            if (TypeOfCommand == TypeOfCommand.Set_Video_Mute_User_Command || TypeOfCommand == TypeOfCommand.User_Join_Media_Channel_Command ||
                TypeOfCommand == TypeOfCommand.User_Video_Muted_Command)
            {
                toReturn += $"IsVideoMuted: {this.IsVideoMuted}" + Environment.NewLine;
            }
            if (TypeOfCommand == TypeOfCommand.Set_Deafen_User_Command || TypeOfCommand == TypeOfCommand.User_Join_Media_Channel_Command ||
                TypeOfCommand == TypeOfCommand.User_Deafened_Command)
            {
                toReturn += $"IsDeafened: {this.IsDeafened}" + Environment.NewLine;
            }
            // Check collections
            if (this.MessagesOfAChatRoom != null && this.MessagesOfAChatRoom.Count > 0)
            {
                toReturn += $"MessagesOfAChatRoom: [Collection with {this.MessagesOfAChatRoom.Count} items]" + Environment.NewLine;
            }

            if (this.UsersMediaConnectionDetails != null && this.UsersMediaConnectionDetails.Count > 0)
            {
                toReturn += $"UsersMediaConnectionDetails: [Collection with {this.UsersMediaConnectionDetails.Count} items]" + Environment.NewLine;
            }

            if (this.AllUsersDetails != null && this.AllUsersDetails.Count > 0)
            {
                toReturn += $"AllUsersDetails: [Collection with {this.AllUsersDetails.Count} items]" + Environment.NewLine;

            }
            return toReturn;
        }
    }
}
