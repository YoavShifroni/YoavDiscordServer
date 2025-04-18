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

        private void HandleSendMessage(string messageThatTheUserSent, int chatRoomId)
        {
            RoomsManager.HandleMessageSentFromUserInChat(this._userId,this.Username, messageThatTheUserSent,
                chatRoomId);
        }

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
        /// call the GetMessageHistoryOfChatRoom function in the RoomManager class
        /// </summary>
        /// <param name="chatRoomId"></param>
        private void HandleGetMessagesHistoryOfChatRoom(int chatRoomId) 
        {
            RoomsManager.GetMessagesHistoryOfChatRoom(this._userId, chatRoomId);
        }

        private void HandleConnectToMediaRoom(int mediaRoomId, int mediaPort)
        {
            MediaRoom mediaRoom = RoomsManager.GetMediaRoomById(mediaRoomId);
            mediaRoom.AddUser(this._userId, mediaPort);
            RoomsManager.UpdateOthersWhenNewParticipantJoinTheMediaRoom(this._userId,this.Username ,mediaRoom);
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

        public void RemoveUserFromAllMediaRooms()
        {
            this.HandleDisconnectFromMediaRoom(RoomsManager.GetMediaRoomIdForUser(this._userId));
            this.UpdateOthersAboutAllUsersStatus();
        }

        private void HandleFetchAllUsers()
        {
            List<UserDetails> details = this.GetAccurateAllUsersDetails();
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Get_All_Users_Details_Command;
            clientServerProtocol.AllUsersDetails = details;
            Console.WriteLine("Message sent to client: " + clientServerProtocol.ToString());
            this._connection.SendMessage(ClientServerProtocolParser.Generate(clientServerProtocol));
        }  

        private void UpdateOthersAboutAllUsersStatus()
        {
            List<UserDetails> details = this.GetAccurateAllUsersDetails();
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Get_All_Users_Details_Command;
            clientServerProtocol.AllUsersDetails = details;
            DiscordClientConnection.SendMessageToAllUserExceptOne(this._userId, clientServerProtocol);
        }


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
