using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public class CommandHandlerForSingleUser
    {
        private int _userId;

        private SqlConnect _sqlConnect;

        private DiscordClientConnection _connection;


        public CommandHandlerForSingleUser(DiscordClientConnection connection)
        {
            this._sqlConnect = new SqlConnect();
            this._connection = connection;
        }


        public void HandleCommand(string messageFromClient)
        {
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol(messageFromClient);
            switch(clientServerProtocol.TypeOfCommand) 
            {

                case TypeOfCommand.Login_Command:
                    this.HandleLogin(clientServerProtocol.Username, clientServerProtocol.Password);
                    break;

                case TypeOfCommand.Registration_Command:
                    this.HandleRegistration(clientServerProtocol.Username, clientServerProtocol.Password, clientServerProtocol.FirstName,
                        clientServerProtocol.LastName, clientServerProtocol.Email, clientServerProtocol.City, clientServerProtocol.Gender);
                    break;

                case TypeOfCommand.Forgot_Password_Command:
                    this.HandleForgotPassword(clientServerProtocol.Username, clientServerProtocol.Code);
                    break;

                case TypeOfCommand.Update_Password_Command:
                    this.HandleUpdatePassword(clientServerProtocol.Username, clientServerProtocol.Password);
                    break;

                case TypeOfCommand.Profile_Picture_Selected_Command:
                    this.HandleProfilePictureSelected(clientServerProtocol.ProfilePicture);
                    break;

            }
        }

        

        private void HandleLogin(string username,  string password)
        {
            string hashPassword = CommandHandlerForSingleUser.CreateSha256(password);
            this._userId = this._sqlConnect.GetUserId(username, hashPassword);
            if (this._userId <= 0)
            {
                ClientServerProtocol protocol = new ClientServerProtocol();
                protocol.TypeOfCommand = TypeOfCommand.Error_Command;
                protocol.Message = "Wrong username or password";
                this._connection.SendMessage(protocol.Generate());
                return;
            }
            string email = this._sqlConnect.GetEmail(username);
            string codeToEmail = this.GetRandomCode();
            //Console.WriteLine("The Code is: " + codeToEmail);
            this.SendEmail(email, codeToEmail);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Code_Sent_To_Email_Command;
            clientServerProtocol.Code = codeToEmail;
            this._connection.SendMessage(clientServerProtocol.Generate());

        }


        private void HandleRegistration(string username, string password, string firstName, string lastName, string email, string city, string gender)
        {
            if (this._sqlConnect.IsExist(username))
            {
                ClientServerProtocol protocol = new ClientServerProtocol();
                protocol.TypeOfCommand = TypeOfCommand.Error_Command;
                protocol.Message = "username already exists";
                this._connection.SendMessage(protocol.Generate());
                return;
            }
            string hashPassword = CommandHandlerForSingleUser.CreateSha256(password);
            this._userId = this._sqlConnect.InsertNewUser(username, hashPassword, firstName, lastName, email, city, gender);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Successes_Registration_Command;
            this._connection.SendMessage(clientServerProtocol.Generate());
        }

        /// <summary>
        /// the function use the hash function "SHA256" and return and return the hash string
        /// I took this function from the website StackOverFlow in this link: 
        /// https://stackoverflow.com/questions/16999361/obtain-sha-256-string-of-a-string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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

        private string GetRandomCode()
        {
            var charsALL = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz#?!@$%^&*-";
            var randomIns = new Random();
            var rndChars = Enumerable.Range(0, 6)
                            .Select(_ => charsALL[randomIns.Next(charsALL.Length)])
                            .ToArray();
            return new string(rndChars);
        }


        private void HandleForgotPassword(string username, string code)
        {
            if(!this._sqlConnect.IsExist(username))
            {
                return;
            }

            string email = this._sqlConnect.GetEmail(username);
            this.SendEmail(email, code);
        }


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

        private void HandleUpdatePassword(string username, string password)
        {
            string hashPassword = CommandHandlerForSingleUser.CreateSha256(password);
            this._sqlConnect.UpdatePassword(username, hashPassword);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Login_Command;
            this._connection.SendMessage(clientServerProtocol.Generate());

        }

        private void HandleProfilePictureSelected(byte[] profilePicture)
        {
            this._sqlConnect.UpdateProfilePictureByUserId(profilePicture, this._userId);
            ClientServerProtocol clientServerProtocol = new ClientServerProtocol();
            clientServerProtocol.TypeOfCommand = TypeOfCommand.Enter_Yoav_Discord_Command;
            this._connection.SendMessage(clientServerProtocol.Generate());
        }
    }
}
