using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    /// <summary>
    /// Enum that represent the type of command that will be sent to the client using the protocol
    /// </summary>
    public enum TypeOfCommand
    {
        Registration_Command,
        Login_Command,
        Check_If_Username_Already_Exist_Command,
        Code_Sent_To_Email_Command,
        Successes_Username_Not_In_The_System_Command,
        Successes_Connected_To_The_Application_Command,
        Successes_Forgot_Password_Command,
        Error_Command,
        Forgot_Password_Command,
        Update_Password_Command,
        Profile_Picture_Selected_Command,
        Login_Cooldown_Command,
        Get_Username_And_Profile_Picture_Command


    }
}
