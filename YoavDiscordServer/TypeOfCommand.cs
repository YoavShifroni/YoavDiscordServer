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
        Success_Username_Not_In_The_System_Command,
        Success_Connected_To_The_Application_Command,
        Success_Forgot_Password_Command,
        Error_Command,
        Forgot_Password_Command,
        Update_Password_Command,
        Profile_Picture_Selected_Command,
        Login_Cooldown_Command,
        Get_Username_And_Profile_Picture_Command,
        Send_Message_Command,
        Message_From_Other_User_Command,
        Fetch_Image_Of_User_Command,
        Return_Image_Of_User_Command,
        Get_Messages_History_Of_Chat_Room_Command,
        Return_Messages_History_Of_Chat_Room_Command,
        Connect_To_Media_Room_Command,
        New_Participant_Join_The_Media_Room_Command,
        Get_All_Ips_Of_Connected_Users_In_Some_Media_Room_Command,
        Disconnect_From_Media_Room_Command,
        Some_User_Left_The_Media_Room_Command,
        Fetch_All_Users_Command,
        Get_All_Users_Details_Command,
        User_Join_Media_Channel_Command,
        User_Leave_Media_Channel_Command,
        Set_Mute_User_Command,
        Set_Deafen_User_Command,
        Disconnect_User_From_Media_Room_Command,
        User_Muted_Command,
        User_Deafened_Command,
        User_Disconnected_Command,
    }
}
