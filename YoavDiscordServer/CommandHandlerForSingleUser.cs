using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;

namespace YoavDiscordServer
{
    /// <summary>
    /// Handles commands for a single user in the Discord server.
    /// </summary>
    public class CommandHandlerForSingleUser
    {
        /// <summary>
        /// The ID of the user.
        /// </summary>
        public int _userId;

        /// <summary>
        /// The count of consecutive failed login attempts by the user.
        /// </summary>
        private int _countLoginFailures;


        public string Username;

        private byte[] _profilePicture;

        /// <summary>
        /// The maximum number of allowed failed login attempts before applying a cooldown.
        /// </summary>
        private const int MAX_NUMBER_OF_LOGIN_FAILED_ATTEMPTS = 10;

        /// <summary>
        /// Logger instance for user-specific logging.
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// Instance to handle database operations for user-related commands.
        /// </summary>
        private SqlConnect _sqlConnect;

        /// <summary>
        /// The connection associated with the Discord client.
        /// </summary>
        private DiscordClientConnection _connection;


        public bool IsAuthenticated = false;

        public int Role;

        /// <summary>
        /// Constructor with parameter
        /// </summary>
        /// <param name="connection"></param>
        public CommandHandlerForSingleUser(DiscordClientConnection connection)
        {
            this._countLoginFailures = 0;
            this._sqlConnect = new SqlConnect();
            this._connection = connection;
        }

        /// <summary>
        /// Handles the incoming command from the client and routes it to the appropriate handler.
        /// </summary>
        /// <param name="messageFromClient"></param>
        public void HandleCommand(string messageFromClient)
        {
            ClientServerProtocol clientServerProtocol = ClientServerProtocolParser.Parse(messageFromClient);
            Console.WriteLine("Received from client: " + clientServerProtocol.ToString());
            switch (clientServerProtocol.TypeOfCommand)
            {
                case TypeOfCommand.Login_Command:
                    this.HandleLogin(clientServerProtocol.Username, clientServerProtocol.Password);
                    break;
                case TypeOfCommand.Registration_Command:
                    this.HandleRegistration(clientServerProtocol.Username, clientServerProtocol.Password, clientServerProtocol.FirstName,
                        clientServerProtocol.LastName, clientServerProtocol.Email, clientServerProtocol.City, clientServerProtocol.Gender,
                        clientServerProtocol.ProfilePicture);
                    break;
                case TypeOfCommand.Check_If_Username_Already_Exist_Command:
                    this.HandleCheckIfUsernameAlreadyExistCommand(clientServerProtocol.Username);
                    break;
                case TypeOfCommand.Forgot_Password_Command:
                    this.HandleForgotPassword(clientServerProtocol.Username, clientServerProtocol.Code);
                    break;
                case TypeOfCommand.Update_Password_Command:
                    this.HandleUpdatePassword(clientServerProtocol.Username, clientServerProtocol.Password);
                    break;

                case TypeOfCommand.Get_Username_And_Profile_Picture_Command:
                    this.HandleGetUsernameAndProfilePicture();
                    break;

                case TypeOfCommand.Send_Message_Command:
                    this.HandleSendMessage(clientServerProtocol.MessageThatTheUserSent, clientServerProtocol.ChatRoomId);
                    break;

                case TypeOfCommand.Fetch_Image_Of_User_Command:
                    this.HandleFetchImageOfUser(clientServerProtocol.UserId);
                    break;

                case TypeOfCommand.Get_Messages_History_Of_Chat_Room_Command:
                    this.HandleGetMessagesHistoryOfChatRoom(clientServerProtocol.ChatRoomId);
                    break;

                case TypeOfCommand.Connect_To_Media_Room_Command:
                    this.HandleConnectToMediaRoom(clientServerProtocol.MediaRoomId, clientServerProtocol.MediaPort);
                    break;

                case TypeOfCommand.Disconnect_From_Media_Room_Command:
                    this.HandleDisconnectFromMediaRoom(clientServerProtocol.MediaRoomId);
                    break;

                case TypeOfCommand.Fetch_All_Users_Command:
                    this.HandleFetchAllUsers();
                    break;

                case TypeOfCommand.Set_Mute_User_Command:
                    this.HandleSetMuteUser(clientServerProtocol.UserId, clientServerProtocol.IsMuted);
                    break;

                case TypeOfCommand.Set_Deafen_User_Command:
                    this.HandleSetDeafenUser(clientServerProtocol.UserId, clientServerProtocol.IsDeafened);
                    break;

                case TypeOfCommand.Disconnect_User_From_Media_Room_Command:
                    this.HandleDisconnectUserFromMediaRoom(clientServerProtocol.UserId, clientServerProtocol.MediaRoomId);
                    break;

                case TypeOfCommand.Set_Video_Mute_User_Command:
                    this.HandleSetVideoMuteUser(clientServerProtocol.UserId, clientServerProtocol.IsVideoMuted);
                    break;

                case TypeOfCommand.Update_User_Role_Command:
                    this.HandleUpdateUserRole(clientServerProtocol.UserId, clientServerProtocol.Role);
                    break;



            }
        }

        




        /// <summary>
        /// Handles the login process, including validation and cooldowns for failed attempts.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        private void HandleLogin(string username, string password)
        {
            string hashPassword = CommandHandlerForSingleUser.CreateSha256(password);
            this._userId = this._sqlConnect.GetUserId(username, hashPassword);
            bool isAlreadyConnected = DiscordClientConnection.CheckIfUserAlreadyConnected(this._userId);
            if (isAlreadyConnected)
            {
                ClientServerProtocol clientServerProtocol1 = new ClientServerProtocol();
                clientServerProtocol1.TypeOfCommand = TypeOfCommand.Error_Command;
                clientServerProtocol1.ErrorMessage = "This user is already connected";
                this._connection.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol1));
                //Thread.Sleep(2000);
                //this._connection.Disconnect();
                return;

            }
            this.Role = this._sqlConnect.GetUserRole(this._userId);
            if (this._userId <= 0)
            {
                this._countLoginFailures++;
                ClientServerProtocol protocol = new ClientServerProtocol();
                if (this._countLoginFailures >= MAX_NUMBER_OF_LOGIN_FAILED_ATTEMPTS)
                {
                    protocol.TypeOfCommand = TypeOfCommand.Login_Cooldown_Command;
                    protocol.TimeToCooldown = this.CalculateTimeToCooldown(this._countLoginFailures);
                    protocol.ErrorMessage = String.Format("Too many failed attempts to login, please wait {0} " +
                        "minutes", protocol.TimeToCooldown);
                }
                else
                {
                    protocol.TypeOfCommand = TypeOfCommand.Error_Command;
                    protocol.ErrorMessage = "Wrong username or password";
                }
                Console.WriteLine("Message sent to client: " + protocol.ToString());
                this._connection.SendMessage(ClientServerProtocolParser.Generate(protocol));
                return;
            }
            this._countLoginFailures = 0;
            this.Username = username;
            string email = this._sqlConnect.GetEmail(username);
            string codeToEmail = this.GetRandomCode();
            this.SendEmail(email, codeToEmail);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Code_Sent_To_Email_Command;
            clientServerProtocol.Code = codeToEmail;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            this._connection.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
            this._logger = UserLogger.GetLoggerForUser(username);
            this._logger.Info("Successfully logged in");
        }

        /// <summary>
        /// Calculates the cooldown time based on the number of failed login attempts.
        /// </summary>
        /// <param name="countLoginFailures"></param>
        private int CalculateTimeToCooldown(int countLoginFailures)
        {
            if (countLoginFailures == 10)
            {
                return 1;
            }
            return ((countLoginFailures - MAX_NUMBER_OF_LOGIN_FAILED_ATTEMPTS) * 5);
        }

        /// <summary>
        /// Handles checking if a username already exists in the system.
        /// </summary>
        /// <param name="username"></param>
        private void HandleCheckIfUsernameAlreadyExistCommand(string username)
        {
            ClientServerProtocol protocol = new ClientServerProtocol();
            if (this._sqlConnect.IsExist(username))
            {
                protocol.TypeOfCommand = TypeOfCommand.Error_Command;
                protocol.ErrorMessage = "Username already exists";
                this._connection.SendMessage(ClientServerProtocolParser.Generate(protocol));
                return;
            }
            protocol.TypeOfCommand = TypeOfCommand.Success_Username_Not_In_The_System_Command;
            Console.WriteLine("Message sent to client: " + protocol.ToString());
            this._connection.SendMessage(ClientServerProtocolParser.Generate(protocol));
        }

        /// <summary>
        /// Handles the user registration process.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="email"></param>
        /// <param name="city"></param>
        /// <param name="gender"></param>
        /// <param name="imageToByteArray"></param>
        private void HandleRegistration(string username, string password, string firstName, string lastName, string email,
            string city, string gender, byte[] imageToByteArray)
        {
            if (this._sqlConnect.IsExist(username))
            {
                ClientServerProtocol protocol = new ClientServerProtocol();
                protocol.TypeOfCommand = TypeOfCommand.Error_Command;
                protocol.ErrorMessage = "Username already exists";
                Console.WriteLine("Message sent to client: " + protocol.ToString());
                this._connection.SendMessage(ClientServerProtocolParser.Generate(protocol));
                return;
            }
            string hashPassword = CommandHandlerForSingleUser.CreateSha256(password);
            this._userId = this._sqlConnect.InsertNewUser(username, hashPassword, firstName, lastName, email, city, gender, imageToByteArray);
            this.Username = username;
            this._profilePicture = imageToByteArray;
            this.Role = 2;
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Success_Connected_To_The_Application_Command;
            clientServerProtocol.ProfilePicture = imageToByteArray;
            clientServerProtocol.Username = username;
            clientServerProtocol.UserId = this._userId;
            clientServerProtocol.Role = this.Role;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            this._connection.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
            this.UpdateOthersAboutAllUsersStatus();
            BotManager.GetInstance().NotifyUserRegistered(this._userId, username);
            this._logger = UserLogger.GetLoggerForUser(username);
            this._logger.Info("Successfully registered");
            this.IsAuthenticated = true;
        }

        /// <summary>
        /// Hashes a string using SHA256.
        /// </summary>
        /// <param name="value"></param>
        public static string CreateSha256(string value)
        {
            StringBuilder Sb = new StringBuilder();

            using (var hash = SHA256.Create())
            {
                Encoding enc = Encoding.UTF8;
                byte[] result = hash.ComputeHash(enc.GetBytes(value));

                foreach (byte b in result)
                    Sb.Append(b.ToString("x2"));
            }

            return Sb.ToString();
        }

        /// <summary>
        /// Generates a random code consisting of letters, digits, and special characters.
        /// </summary>
        private string GetRandomCode()
        {
            var charsALL = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz#?!@$%^&*-";
            var randomIns = new Random();
            var rndChars = Enumerable.Range(0, 6)
                            .Select(_ => charsALL[randomIns.Next(charsALL.Length)])
                            .ToArray();
            return new string(rndChars);
        }

        /// <summary>
        /// Handles the forgot password process, including sending a code to the user's email.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="code"></param>
        private void HandleForgotPassword(string username, string code)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            if (!this._sqlConnect.IsExist(username))
            {
                clientServerProtocol.TypeOfCommand = TypeOfCommand.Error_Command;
                clientServerProtocol.ErrorMessage = "The username isn't exist in the system, please check the username that you entered";
            }
            else
            {
                this.Username = username;
                string email = this._sqlConnect.GetEmail(username);
                this.SendEmail(email, code);
                this._logger = UserLogger.GetLoggerForUser(username);
                this._logger.Info("Forgot password email sent");
                clientServerProtocol.TypeOfCommand = TypeOfCommand.Success_Forgot_Password_Command;
            }
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            this._connection.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
            
           
        }

        /// <summary>
        /// Sends an email with the provided code to the recipient.
        /// </summary>
        /// <param name="recipientEmail"></param>
        /// <param name="code"></param>
        private void SendEmail(string email, string code)
        {
            // Command-line argument must be the SMTP host.
            SmtpClient smtpClient = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential("yudassin@gmail.com", "livv ckoy dtyo sqjp\r\n")
            };
            MailMessage msg = new System.Net.Mail.MailMessage("yudassin@gmail.com", email
                , "Code For Yoav Discord", "Your code is: " + code);
            smtpClient.SendMailAsync(msg);
        }

        /// <summary>
        /// Updates the password for the user.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        private void HandleUpdatePassword(string username, string password)
        {
            if (!this._sqlConnect.IsExist(username))
            {
                return;
            }
            string hashNewPassword = CommandHandlerForSingleUser.CreateSha256(password);
            string currentPassword = this._sqlConnect.GetPassword(username);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            if(currentPassword == hashNewPassword)
            {
                clientServerProtocol.TypeOfCommand = TypeOfCommand.Error_Command;
                clientServerProtocol.ErrorMessage = "the password that you entered is your past password, " +
                    "please enter different password or just login with this password";
            }
            else
            {
                this._sqlConnect.UpdatePassword(username, hashNewPassword);
                this._userId = this._sqlConnect.GetUserId(username, hashNewPassword);
                this.Role = this._sqlConnect.GetUserRole(this._userId);
                this._logger = UserLogger.GetLoggerForUser(username);
                this._logger.Info("Password updated successfully");
                clientServerProtocol.TypeOfCommand = TypeOfCommand.Success_Connected_To_The_Application_Command;
                this._profilePicture = this._sqlConnect.GetProfilePictureByUsername(username);
                clientServerProtocol.ProfilePicture = this._profilePicture;
                clientServerProtocol.Username = username;
                clientServerProtocol.UserId = this._userId;
                clientServerProtocol.Role = this.Role;
                this.IsAuthenticated = true;
                this.UpdateOthersAboutAllUsersStatus();
            }
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            this._connection.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
            
        }

        /// <summary>
        /// Processes a request for the user's username and profile picture.
        /// Retrieves the user's profile data and sends it to the client.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Creates a Success_Connected_To_The_Application_Command response
        /// 2. Retrieves the user's profile picture from the database
        /// 3. Sends the user's profile information, username, ID, and role to the client
        /// 4. Sets the user's authentication status to true
        /// 5. Notifies other users about the updated user status list
        /// 
        /// This is typically called after successful authentication to initialize
        /// the client's user information.
        /// </remarks>
        private void HandleGetUsernameAndProfilePicture()
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Success_Connected_To_The_Application_Command;
            this._profilePicture = this._sqlConnect.GetProfilePictureByUsername(this.Username);
            clientServerProtocol.ProfilePicture = this._profilePicture;
            clientServerProtocol.Username = this.Username;
            clientServerProtocol.UserId = this._userId;
            clientServerProtocol.Role = this.Role;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            this._connection.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
            this.IsAuthenticated = true;
            this.UpdateOthersAboutAllUsersStatus();
        }

        /// <summary>
        /// Handles a message sent by a user in a chat room.
        /// Forwards the message to the RoomsManager for processing and distribution.
        /// </summary>
        /// <param name="messageThatTheUserSent">The content of the message sent by the user.</param>
        /// <param name="chatRoomId">The ID of the chat room where the message was sent.</param>
        /// <remarks>
        /// This method delegates message handling to the RoomsManager, which is responsible
        /// for distributing the message to all users in the specified chat room.
        /// </remarks>
        private void HandleSendMessage(string messageThatTheUserSent, int chatRoomId)
        {
            RoomsManager.HandleMessageSentFromUserInChat(this._userId, this.Username, messageThatTheUserSent,
                chatRoomId);
        }

        /// <summary>
        /// Processes a request for a user's profile picture.
        /// Retrieves the requested user's profile picture and sends it to the requesting client.
        /// </summary>
        /// <param name="userId">The ID of the user whose profile picture is being requested.</param>
        /// <remarks>
        /// This method:
        /// 1. Retrieves the requested profile picture from the database
        /// 2. Creates a Return_Image_Of_User_Command response
        /// 3. Sends the profile picture data to the requesting client
        /// 
        /// This is typically called when a client needs to display another user's
        /// profile picture that it doesn't have cached locally.
        /// </remarks>
        private void HandleFetchImageOfUser(int userId)
        {
            byte[] someUserProfilePicture = this._sqlConnect.GetProfilePictureByUserId(userId);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Return_Image_Of_User_Command;
            clientServerProtocol.UserId = userId;
            clientServerProtocol.ProfilePicture = someUserProfilePicture;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            this._connection.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
        }

        /// <summary>
        /// Handles a request for the message history of a specific chat room.
        /// Retrieves and sends the chat history to the requesting client.
        /// </summary>
        /// <param name="chatRoomId">The ID of the chat room whose history is requested.</param>
        /// <remarks>
        /// This method delegates to the RoomsManager to retrieve and send the message history
        /// for the specified chat room to the requesting user.
        /// 
        /// This is typically called when a user joins or switches to a chat room,
        /// allowing them to see previous messages.
        /// </remarks>
        private void HandleGetMessagesHistoryOfChatRoom(int chatRoomId)
        {
            RoomsManager.GetMessagesHistoryOfChatRoom(this._userId, chatRoomId);
        }

        /// <summary>
        /// Handles a user's request to connect to a media (voice/video) room.
        /// Updates room membership and notifies all users about the new participant.
        /// </summary>
        /// <param name="mediaRoomId">The ID of the media room to connect to.</param>
        /// <param name="mediaPort">The port on which the user is listening for media connections.</param>
        /// <remarks>
        /// This method:
        /// 1. Adds the user to the specified media room with their media port
        /// 2. Notifies other users in the room about the new participant
        /// 3. Notifies the joining user about existing participants in the room
        /// 4. Broadcasts a User_Join_Media_Channel_Command to all users except the joining user
        /// 
        /// The mediaPort parameter is critical for establishing peer-to-peer connections
        /// between users in the media room.
        /// </remarks>
        private void HandleConnectToMediaRoom(int mediaRoomId, int mediaPort)
        {
            MediaRoom mediaRoom = RoomsManager.GetMediaRoomById(mediaRoomId);
            mediaRoom.AddUser(this._userId, mediaPort);
            RoomsManager.UpdateOthersWhenNewParticipantJoinTheMediaRoom(this._userId, this.Username, mediaRoom);
            RoomsManager.UpdateNewUserAboutTheCurrentUsersInTheMediaRoom(this._userId, mediaRoom);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.User_Join_Media_Channel_Command;
            clientServerProtocol.UserId = this._userId;
            clientServerProtocol.MediaRoomId = mediaRoomId;
            clientServerProtocol.Username = this.Username;
            clientServerProtocol.ProfilePicture = this._profilePicture;
            clientServerProtocol.Role = this.Role;
            clientServerProtocol.IsMuted = RoomsManager.IsUserMuted(this._userId);
            clientServerProtocol.IsDeafened = RoomsManager.IsUserDeafened(this._userId);
            clientServerProtocol.IsVideoMuted = RoomsManager.IsUserVideoMuted(this._userId);
            DiscordClientConnection.SendMessageToAllUserExceptOne(this._userId, clientServerProtocol);
        }

        /// <summary>
        /// Handles a user's request to disconnect from a media room.
        /// Updates room membership and notifies all users about the departure.
        /// </summary>
        /// <param name="mediaRoomId">The ID of the media room to disconnect from.</param>
        /// <remarks>
        /// This method:
        /// 1. Validates that the specified media room exists
        /// 2. Notifies other users in the room about the user's departure
        /// 3. Broadcasts a User_Leave_Media_Channel_Command to all users except the leaving user
        /// 
        /// If the specified media room doesn't exist, the method returns without taking action.
        /// </remarks>
        private void HandleDisconnectFromMediaRoom(int mediaRoomId)
        {
            MediaRoom mediaRoom = RoomsManager.GetMediaRoomById(mediaRoomId);
            if (mediaRoom == null)
            {
                return;
            }
            RoomsManager.UpdateEveryoneTheSomeUserLeft(this._userId, mediaRoom);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.User_Leave_Media_Channel_Command;
            clientServerProtocol.UserId = this._userId;
            clientServerProtocol.MediaRoomId = mediaRoomId;
            DiscordClientConnection.SendMessageToAllUserExceptOne(this._userId, clientServerProtocol);

        }

        /// <summary>
        /// Removes the user from all media rooms and updates other users about the change.
        /// Typically called when a user disconnects from the server.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Finds which media room (if any) the user is currently in
        /// 2. Calls HandleDisconnectFromMediaRoom to handle the media room departure
        /// 3. Updates all other connected users about the updated user status list
        /// 
        /// This ensures clean disconnection from all active media sessions when
        /// a user leaves the application.
        /// </remarks>
        public void RemoveUserFromAllMediaRooms()
        {
            this.HandleDisconnectFromMediaRoom(RoomsManager.GetMediaRoomIdForUser(this._userId));
            this.UpdateOthersAboutAllUsersStatus();
        }

        /// <summary>
        /// Handles a request for information about all users.
        /// Retrieves and sends a list of all users with their current status to the requesting client.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Gets accurate user details including online status and media room membership
        /// 2. Creates a Get_All_Users_Details_Command response
        /// 3. Sends the complete user list to the requesting client
        /// 
        /// This is typically called when a client initializes or refreshes its user list.
        /// </remarks>
        private void HandleFetchAllUsers()
        {
            List<UserDetails> details = this.GetAccurateAllUsersDetails();
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Get_All_Users_Details_Command;
            clientServerProtocol.AllUsersDetails = details;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            this._connection.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
        }

        /// <summary>
        /// Notifies all other connected users about changes in the user list.
        /// Sends an updated list of all users with their current status to all clients except the current user.
        /// </summary>
        /// <remarks>
        /// This method:
        /// 1. Gets accurate user details including online status and media room membership
        /// 2. Creates a Get_All_Users_Details_Command message
        /// 3. Broadcasts the message to all connected users except the current user
        /// 
        /// This is called whenever a user's status changes (connecting, disconnecting,
        /// joining or leaving media rooms) to keep all clients' user lists synchronized.
        /// </remarks>
        private void UpdateOthersAboutAllUsersStatus()
        {
            List<UserDetails> details = this.GetAccurateAllUsersDetails();
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Get_All_Users_Details_Command;
            clientServerProtocol.AllUsersDetails = details;
            DiscordClientConnection.SendMessageToAllUserExceptOne(this._userId, clientServerProtocol);
        }

        /// <summary>
        /// Gets a list of all users with accurate status information.
        /// Enriches basic user data with online status and current media room membership.
        /// </summary>
        /// <returns>A list of UserDetails objects with up-to-date status information.</returns>
        /// <remarks>
        /// This method:
        /// 1. Retrieves basic user information from the database
        /// 2. Updates each user's MediaChannelId based on current media room membership
        /// 3. Sets each user's Status flag based on whether they're currently connected
        /// 
        /// This provides a complete and accurate view of all users in the system
        /// including those who are currently offline.
        /// </remarks>
        private List<UserDetails> GetAccurateAllUsersDetails()
        {
            List<UserDetails> details = this._sqlConnect.GetAllUsersDetails();
            List<int> connectedIds = DiscordClientConnection.GetIdsOfAllConnectedUsers();
            foreach (UserDetails userDetails in details)
            {
                userDetails.MediaChannelId = RoomsManager.GetMediaRoomIdForUser(userDetails.UserId);
                if (connectedIds.Contains(userDetails.UserId))
                {
                    userDetails.Status = true;
                }
                else
                {
                    userDetails.Status = false;
                }
            }
            return details;
        }

        /// <summary>
        /// Handles a request to change a user's audio mute status.
        /// Updates the user's mute state and notifies all clients about the change.
        /// </summary>
        /// <param name="userId">The ID of the user whose mute status is being changed.</param>
        /// <param name="isMuted">True to mute the user, false to unmute.</param>
        /// <remarks>
        /// This method:
        /// 1. Updates the user's mute status in the RoomsManager
        /// 2. Creates a User_Muted_Command message
        /// 3. Broadcasts the status change to all connected clients
        /// 4. Logs the action if one user is muting another (not self-muting)
        /// 
        /// This supports both self-muting and moderator-initiated muting of other users.
        /// </remarks>
        private void HandleSetMuteUser(int userId, bool isMuted)
        {

            RoomsManager.SetUserMuted(userId, isMuted);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.User_Muted_Command;
            clientServerProtocol.UserId = userId;
            clientServerProtocol.IsMuted = isMuted;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            DiscordClientConnection.Broadcast(ClientServerProtocolParser.Generate(clientServerProtocol));

            if (isMuted && userId != this._userId)
            {
                string targetUsername = this._sqlConnect.GetUsernameById(userId);
                this._logger.Info($"User {this.Username} muted user {targetUsername}");
            }
        }

        /// <summary>
        /// Handles a request to change a user's deafen status.
        /// Updates the user's deafen state and notifies all clients about the change.
        /// </summary>
        /// <param name="userId">The ID of the user whose deafen status is being changed.</param>
        /// <param name="isDeafened">True to deafen the user, false to undeafen.</param>
        /// <remarks>
        /// This method:
        /// 1. Updates the user's deafen status in the RoomsManager
        /// 2. Creates a User_Deafened_Command message
        /// 3. Broadcasts the status change to all connected clients
        /// 4. Logs the action if one user is deafening another (not self-deafening)
        /// 
        /// Deafening prevents a user from hearing any audio in media rooms.
        /// This supports both self-deafening and moderator-initiated deafening of other users.
        /// </remarks>
        private void HandleSetDeafenUser(int userId, bool isDeafened)
        {
            RoomsManager.SetUserDeafened(userId, isDeafened);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.User_Deafened_Command;
            clientServerProtocol.UserId = userId;
            clientServerProtocol.IsDeafened = isDeafened;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            DiscordClientConnection.Broadcast(ClientServerProtocolParser.Generate(clientServerProtocol));

            // Log this action if it's not self-deafening
            if (isDeafened && userId != this._userId)
            {
                string targetUsername = this._sqlConnect.GetUsernameById(userId);
                this._logger.Info($"User {this.Username} deafened user {targetUsername}");
            }
        }

        /// <summary>
        /// Handles a request to forcibly disconnect a user from a media room.
        /// Typically used by moderators to remove users from voice/video channels.
        /// </summary>
        /// <param name="userId">The ID of the user to disconnect.</param>
        /// <param name="mediaRoomId">The ID of the media room from which to disconnect the user.</param>
        /// <remarks>
        /// This method:
        /// 1. Validates that the specified media room exists
        /// 2. Confirms that the target user is actually in that media room
        /// 3. Broadcasts a User_Disconnected_Command to all clients
        /// 4. Updates the room state by removing the user
        /// 5. Logs the action if one user is disconnecting another
        /// 
        /// If the room doesn't exist or the user isn't in the specified room,
        /// the method returns without taking action.
        /// </remarks>
        private void HandleDisconnectUserFromMediaRoom(int userId, int mediaRoomId)
        {
            // Check if the user is in the specified media room
            MediaRoom mediaRoom = RoomsManager.GetMediaRoomById(mediaRoomId);
            if (mediaRoom == null)
            {
                return; // Room doesn't exist
            }

            if (!mediaRoom.UsersInThisRoom.ContainsKey(userId))
            {
                return; // User is not in this room
            }

            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.User_Disconnected_Command;
            clientServerProtocol.UserId = userId;
            clientServerProtocol.MediaRoomId = mediaRoomId;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            DiscordClientConnection.Broadcast(ClientServerProtocolParser.Generate(clientServerProtocol));
            // Update the room's state - remove the user
            RoomsManager.UpdateEveryoneTheSomeUserLeft(userId, mediaRoom);

            // Log this action
            if (userId != this._userId)
            {
                string targetUsername = this._sqlConnect.GetUsernameById(userId);
                this._logger.Info($"User {this.Username} disconnected user {targetUsername} from media room {mediaRoomId}");
            }
        }

        /// <summary>
        /// Handles a request to change a user's video mute status.
        /// Updates the user's video mute state and notifies all clients about the change.
        /// </summary>
        /// <param name="userId">The ID of the user whose video mute status is being changed.</param>
        /// <param name="isVideoMuted">True to mute the user's video, false to unmute.</param>
        /// <remarks>
        /// This method:
        /// 1. Updates the user's video mute status in the RoomsManager
        /// 2. Creates a User_Video_Muted_Command message
        /// 3. Broadcasts the status change to all connected clients
        /// 4. Logs the action if one user is video-muting another (not self-muting)
        /// 
        /// Video muting prevents a user's camera feed from being transmitted to other participants.
        /// This supports both self-video-muting and moderator-initiated video muting of other users.
        /// </remarks>
        private void HandleSetVideoMuteUser(int userId, bool isVideoMuted)
        {
            RoomsManager.SetUserVideoMuted(userId, isVideoMuted);

            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.User_Video_Muted_Command;
            clientServerProtocol.UserId = userId;
            clientServerProtocol.IsVideoMuted = isVideoMuted;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            DiscordClientConnection.Broadcast(ClientServerProtocolParser.Generate(clientServerProtocol));

            // Log this action if it's not self-muting
            if (isVideoMuted && userId != this._userId)
            {
                string targetUsername = this._sqlConnect.GetUsernameById(userId);
                this._logger.Info($"User {this.Username} video muted user {targetUsername}");
            }
        }

        /// <summary>
        /// Handles a request to update a user's role.
        /// Validates permissions, updates the role in the database, and notifies all clients.
        /// </summary>
        /// <param name="userId">The ID of the user whose role is being updated.</param>
        /// <param name="newRole">The new role ID to assign to the user.</param>
        /// <remarks>
        /// This method:
        /// 1. Checks the current user's permission to change roles based on their own role
        /// 2. If permitted, updates the user's role in the database
        /// 3. Updates the role in the server-side user data
        /// 4. Broadcasts a User_Role_Has_Been_Updated_Command to all clients
        /// 5. Updates all clients with the refreshed user list
        /// 
        /// The permission rules are:
        /// - Admins (role 0) can change users between Moderator (1) and Member (2) roles
        /// - Moderators (role 1) can only promote Members (2) to Moderators (1)
        /// - Members (role 2) cannot change roles
        /// 
        /// If the user doesn't have permission, an error message is sent back to the requesting client.
        /// </remarks>
        private void HandleUpdateUserRole(int userId, int newRole)
        {
            int userCurrentRole = this._sqlConnect.GetUserRole(userId);
            bool canUpdateRole = false;
            if (this.Role == 0) // Admin
            {
                canUpdateRole = (userCurrentRole == 1 && newRole == 2) || (userCurrentRole == 2 && newRole == 1);
            }
            else if (this.Role == 1) // Moderator
            {
                canUpdateRole = (userCurrentRole == 2 && newRole == 1);
            }

            if (!canUpdateRole)
            {
                // Send error message if the user doesn't have permission
                ClientServerProtocol errorProtocol = new ClientServerProtocol();
                errorProtocol.TypeOfCommand = TypeOfCommand.Error_Command;
                errorProtocol.ErrorMessage = "You don't have permission to update this user's role.";
                Console.WriteLine("Message sent to client: " + errorProtocol.ToString());
                this._connection.SendMessage(ClientServerProtocolParser.Generate(errorProtocol));
                return;
            }


            this._sqlConnect.UpdateUserRole(userId, newRole);
            DiscordClientConnection.GetDiscordClientConnectionById(userId).CommandHandlerForSingleUser.Role = newRole;
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.User_Role_Has_Been_Updated_Command;
            clientServerProtocol.UserId = userId;
            clientServerProtocol.Role = newRole;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            DiscordClientConnection.Broadcast(ClientServerProtocolParser.Generate(clientServerProtocol));
            this.UpdateOthersAboutAllUsersStatus();
        }



    }
}
