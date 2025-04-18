using System;
using System.Collections.Generic;
using YoavDiscordServer;

namespace YoavDiscordServer
{
    /// <summary>
    /// Represents a protocol message used to communicate between client and server,
    /// containing user data, commands, and chat/media interaction details.
    /// </summary>
    public class ClientServerProtocol
    {
        /// <summary>
        /// Gets or sets the command type to be processed by the server or client.
        /// </summary>
        public TypeOfCommand TypeOfCommand { get; set; }

        /// <summary>
        /// Gets or sets the username of the user.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password of the user.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the first name of the user.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name of the user.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the city the user is from.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the gender of the user.
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// Gets or sets the error message to be shown to the user.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the verification code sent to the user's email.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the user's profile picture as a byte array.
        /// </summary>
        public byte[] ProfilePicture { get; set; }

        /// <summary>
        /// Gets or sets the cooldown time in minutes for the user.
        /// </summary>
        public int TimeToCooldown { get; set; }

        /// <summary>
        /// Gets or sets the chat message sent by the user.
        /// </summary>
        public string MessageThatTheUserSent { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of when the message was sent.
        /// </summary>
        public DateTime TimeThatTheMessageWasSent { get; set; }

        /// <summary>
        /// Gets or sets the ID of the chat room the message belongs to.
        /// </summary>
        public int ChatRoomId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the media room the user is connected to.
        /// </summary>
        public int MediaRoomId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the list of messages in a specific chat room.
        /// </summary>
        public List<UserMessage> MessagesOfAChatRoom { get; set; }

        /// <summary>
        /// Gets or sets the IP address of a new participant joining a media channel.
        /// </summary>
        public string NewParticipantIp { get; set; }

        /// <summary>
        /// Gets or sets the connection details of users in a media room.
        /// </summary>
        public List<UserMediaConnectionDetails> UsersMediaConnectionDetails { get; set; }

        /// <summary>
        /// Gets or sets the media port used for real-time communication.
        /// </summary>
        public int MediaPort { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the user.
        /// </summary>
        public string UserIp { get; set; }

        /// <summary>
        /// Gets or sets the list of all users and their details.
        /// </summary>
        public List<UserDetails> AllUsersDetails { get; set; }

        /// <summary>
        /// Gets or sets whether the user is muted.
        /// </summary>
        public bool IsMuted { get; set; }

        /// <summary>
        /// Gets or sets whether the user's video is muted.
        /// </summary>
        public bool IsVideoMuted { get; set; }

        /// <summary>
        /// Gets or sets whether the user is deafened (can't hear).
        /// </summary>
        public bool IsDeafened { get; set; }

        /// <summary>
        /// Gets or sets the user's role (e.g., admin, moderator).
        /// </summary>
        public int Role { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="ClientServerProtocol"/> class.
        /// </summary>
        public ClientServerProtocol()
        {

        }


        /// <summary>
        /// Initializes a new instance of the <see cref="ClientServerProtocol"/> class with all parameters.
        /// </summary>
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
