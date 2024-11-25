using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
        private int _userId;

        /// <summary>
        /// The count of consecutive failed login attempts by the user.
        /// </summary>
        private int _countLoginFailures;

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
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol(messageFromClient);
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
            if (this._userId <= 0)
            {
                this._countLoginFailures++;
                ClientServerProtocol protocol = new ClientServerProtocol();
                if (this._countLoginFailures >= MAX_NUMBER_OF_LOGIN_FAILED_ATTEMPTS)
                {
                    protocol.TypeOfCommand = TypeOfCommand.Login_Cooldown_Command;
                    protocol.TimeToCooldown = this.CalculateTimeToCooldown(this._countLoginFailures);
                    protocol.Message = String.Format("Too many failed attempts to login, please wait {0} minutes", protocol.TimeToCooldown);
                }
                else
                {
                    protocol.TypeOfCommand = TypeOfCommand.Error_Command;
                    protocol.Message = "Wrong username or password";
                }
                this._connection.SendMessage(protocol.Generate());
                return;
            }
            this._countLoginFailures = 0;
            string email = this._sqlConnect.GetEmail(username);
            string codeToEmail = this.GetRandomCode();
            this.SendEmail(email, codeToEmail);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol
            {
                TypeOfCommand = TypeOfCommand.Code_Sent_To_Email_Command,
                Code = codeToEmail
            };
            this._connection.SendMessage(clientServerProtocol.Generate());
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
            if (this._sqlConnect.IsExist(username))
            {
                ClientServerProtocol protocol = new ClientServerProtocol
                {
                    TypeOfCommand = TypeOfCommand.Error_Command,
                    Message = "Username already exists"
                };
                this._connection.SendMessage(protocol.Generate());
                return;
            }
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol
            {
                TypeOfCommand = TypeOfCommand.Successes_Username_Not_In_The_System_Command
            };
            this._connection.SendMessage(clientServerProtocol.Generate());
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
                ClientServerProtocol protocol = new ClientServerProtocol
                {
                    TypeOfCommand = TypeOfCommand.Error_Command,
                    Message = "Username already exists"
                };
                this._connection.SendMessage(protocol.Generate());
                return;
            }
            string hashPassword = CommandHandlerForSingleUser.CreateSha256(password);
            this._userId = this._sqlConnect.InsertNewUser(username, hashPassword, firstName, lastName, email, city, gender, imageToByteArray);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol
            {
                TypeOfCommand = TypeOfCommand.Successes_Registration_Command
            };
            this._connection.SendMessage(clientServerProtocol.Generate());
            this._logger = UserLogger.GetLoggerForUser(username);
            this._logger.Info("Successfully registered");
        }

        /// <summary>
        /// Hashes a string using SHA256.
        /// </summary>
        /// <param name="value"></param>
        private static string CreateSha256(string value)
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
            if (!this._sqlConnect.IsExist(username))
            {
                return;
            }

            string email = this._sqlConnect.GetEmail(username);
            this.SendEmail(email, code);
            this._logger = UserLogger.GetLoggerForUser(username);
            this._logger.Info("Forgot password email sent");
        }

        /// <summary>
        /// Sends an email with the provided code to the recipient.
        /// </summary>
        /// <param name="recipientEmail"></param>
        /// <param name="code"></param>
        private void SendEmail(string recipientEmail, string code)
        {
            MailMessage message = new MailMessage();
            SmtpClient smtpClient = new SmtpClient();

            message.To.Add(recipientEmail);
            message.Subject = "Password Reset Code";
            message.Body = $"Your password reset code is: {code}";
            message.From = new MailAddress("admin@yourdomain.com");

            smtpClient.Host = "smtp.your-email.com"; // Replace with your SMTP host
            smtpClient.Port = 587;
            smtpClient.Credentials = new System.Net.NetworkCredential("your-email@yourdomain.com", "your-password");
            smtpClient.EnableSsl = true;

            smtpClient.Send(message);
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

            string hashPassword = CommandHandlerForSingleUser.CreateSha256(password);
            this._sqlConnect.UpdatePassword(username, hashPassword);
            this._logger = UserLogger.GetLoggerForUser(username);
            this._logger.Info("Password updated successfully");
        }
    }
}
