using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public enum TypeOfCommand
    {
        Registration_Command,
        Login_Command,
        Code_Sent_To_Email_Command,
        Enter_Yoav_Discord_Command,
        Successes_Registration_Command,
        Error_Command, 
        Forgot_Password_Command,
        Update_Password_Command,
        Profile_Picture_Selected_Command,


    }
}
