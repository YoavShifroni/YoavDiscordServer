using System;
using YoavDiscordServer;

namespace YoavDiscordServer
{
    public class ClientServerProtocol
    {
        public TypeOfCommand TypeOfCommand { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string City { get; set; }

        public string Gender { get; set; }

        public string Message { get; set; }

        public string Code { get; set; }

        public byte[] ProfilePicture { get; set; }


        public ClientServerProtocol()
        {

        }

        public ClientServerProtocol(string message)
        {
            string[] answer = message.Split('\n');
            Enum.TryParse(answer[0], out TypeOfCommand cmd);
            this.TypeOfCommand = cmd;

            switch (cmd)
            {
                case TypeOfCommand.Error_Command:
                    this.Message = answer[1];
                    break;


                case TypeOfCommand.Login_Command:
                    this.Username = answer[1];
                    this.Password = answer[2];
                    break;

                case TypeOfCommand.Registration_Command:
                    this.Username = answer[1];
                    this.Password = answer[2];
                    this.FirstName = answer[3];
                    this.LastName = answer[4];
                    this.Email = answer[5];
                    this.City = answer[6];
                    this.Gender = answer[7];
                    break;


                case TypeOfCommand.Forgot_Password_Command:
                    this.Username = answer[1];
                    this.Code = answer[2];
                    break;

                case TypeOfCommand.Update_Password_Command:
                    this.Username = answer[1];
                    this.Password = answer[2];
                    break;

                case TypeOfCommand.Code_Sent_To_Email_Command:
                    this.Code = answer[1];
                    break;

                case TypeOfCommand.Enter_Yoav_Discord_Command:
                    break;

                case TypeOfCommand.Successes_Registration_Command:
                    break;

                case TypeOfCommand.Profile_Picture_Selected_Command:
                    this.ProfilePicture = Convert.FromBase64String(answer[1]);
                    break;

            }
        }

        public string Generate()
        {
            string toSend = this.TypeOfCommand.ToString() + "\n";

            switch (this.TypeOfCommand)
            {
                case TypeOfCommand.Error_Command:
                    toSend += this.Message += "\n";
                    break;

                case TypeOfCommand.Login_Command:
                    toSend += this.Username + "\n";
                    toSend += this.Password + "\n";
                    break;

                case TypeOfCommand.Registration_Command:
                    toSend += this.Username + "\n";
                    toSend += this.Password + "\n";
                    toSend += this.FirstName + "\n";
                    toSend += this.LastName + "\n";
                    toSend += this.Email + "\n";
                    toSend += this.City + "\n";
                    toSend += this.Gender + "\n";
                    break;

                case TypeOfCommand.Forgot_Password_Command:
                    toSend += this.Username + "\n";
                    toSend += this.Code + "\n";
                    break;

                case TypeOfCommand.Update_Password_Command:
                    toSend += this.Username + "\n";
                    toSend += this.Password + "\n";
                    break;

                case TypeOfCommand.Code_Sent_To_Email_Command:
                    toSend += this.Code + "\n";
                    break;

                case TypeOfCommand.Enter_Yoav_Discord_Command:
                    break;

                case TypeOfCommand.Successes_Registration_Command:
                    break;

                case TypeOfCommand.Profile_Picture_Selected_Command:
                    toSend += Convert.ToBase64String(this.ProfilePicture) + "\n";
                    break;
            }
            toSend += "\r\n";
            return toSend;
        }
    }
}
