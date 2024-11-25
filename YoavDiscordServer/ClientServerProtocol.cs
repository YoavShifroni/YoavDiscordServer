using System;
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
        public string Message { get; set; }

        /// <summary>
        /// The code that sent to the user mail
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// The profile picture of this user converts to byte's array
        /// </summary>
        public byte[] ProfilePicture { get; set; }


        public int TimeToCooldown { get; set; }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ClientServerProtocol()
        {

        }



        /// <summary>
        /// Constructor with parameter. This constructor generates the ClientServerProtocol instance from the string we're sending over TCP
        /// </summary>
        /// <param name="message"></param>
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

                case TypeOfCommand.Login_Cooldown_Command:
                    this.Message = answer[1];
                    this.TimeToCooldown = Convert.ToInt32(answer[2]);
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
                    this.ProfilePicture = Convert.FromBase64String(answer[8]);
                    break;

                case TypeOfCommand.Check_If_Username_Already_Exist_Command:
                    this.Username = answer[1];
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

                case TypeOfCommand.Successes_Username_Not_In_The_System_Command:
                case TypeOfCommand.Successes_Registration_Command:
                    break;


            }
        }

        /// <summary>
        /// The function generates the string of the protocol that we're sending over TCP
        /// from the fields of the class, based on the command
        /// </summary>
        /// <returns></returns>
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

                case TypeOfCommand.Login_Cooldown_Command:
                    toSend += this.Message + "\n";
                    toSend += this.TimeToCooldown.ToString() + "\n";
                    break;

                case TypeOfCommand.Registration_Command:
                    toSend += this.Username + "\n";
                    toSend += this.Password + "\n";
                    toSend += this.FirstName + "\n";
                    toSend += this.LastName + "\n";
                    toSend += this.Email + "\n";
                    toSend += this.City + "\n";
                    toSend += this.Gender + "\n";
                    toSend += Convert.ToBase64String(this.ProfilePicture) + "\n";
                    break;

                case TypeOfCommand.Check_If_Username_Already_Exist_Command:
                    toSend += this.Username + "\n";
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


                case TypeOfCommand.Successes_Username_Not_In_The_System_Command:
                case TypeOfCommand.Successes_Registration_Command:
                    break;

            }
            toSend += "\r\n";
            return toSend;
        }
    }
}
