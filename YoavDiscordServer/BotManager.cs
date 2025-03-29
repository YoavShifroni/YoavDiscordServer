using Org.BouncyCastle.Pqc.Crypto.Lms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public class BotManager
    {
        // Singleton instance
        private static BotManager _instance;

        // List of all registered bots
        private TranslateBot _translateBot;
        private ModerationBot _moderationBot;


        // Private constructor for singleton pattern
        private BotManager()
        {
            this._moderationBot = new ModerationBot();

            this._translateBot = new TranslateBot();

            Console.WriteLine("Bot Manager initialized and all bots activated.");
        }

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static BotManager GetInstance()
        {
            if (_instance == null)
            {
                _instance = new BotManager();
            }
            return _instance;
          
        }

        /// <summary>
        /// Notifies the moderation bot about a new user registration
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="username">Username</param>
        public void NotifyUserRegistered(int userId, string username)
        {
             this._moderationBot.OnUserRegistered(userId, username);
        }

        /// <summary>
        /// Processes a message through all bots
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="username">Username</param>
        /// <param name="message">Message content</param>
        /// <param name="chatRoomId">Chat room ID</param>
        /// <returns>True if the message was processed by any bot, false otherwise</returns>
        public async Task<bool> ProcessMessage(int userId, string username, string message, int chatRoomId)
        {
            bool isDone = await this._moderationBot.ProcessMessage(userId, username, message, chatRoomId);
            if (isDone)
            {
                return true;
            }
            return await this._translateBot.ProcessMessage(userId, username, message, chatRoomId);
            
        }
    }
}