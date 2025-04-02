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
    }
}
