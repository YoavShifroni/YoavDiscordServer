using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public abstract class Bot
    {
        /// <summary>
        /// The bot's user ID in the system
        /// </summary>
        protected int BotUserId { get; set; }

        /// <summary>
        /// The bot's username
        /// </summary>
        protected string BotUsername { get; set; }

        /// <summary>
        /// The SqlConnect instance for database operations
        /// </summary>
        protected SqlConnect SqlConnect { get; set; }

        /// <summary>
        /// Dictionary of command prefixes and their handlers
        /// </summary>
        protected Dictionary<string, Func<int,string, string, string[], int, Task>> CommandHandlers { get; set; }

        /// <summary>
        /// Prefix for bot commands (e.g., '!', '/', '-')
        /// </summary>
        public string CommandPrefix { get; protected set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="botUsername">Username for the bot</param>
        /// <param name="commandPrefix">Command prefix for the bot</param>
        protected Bot(string botUsername, string commandPrefix)
        {
            BotUsername = botUsername;
            CommandPrefix = commandPrefix;
            SqlConnect = new SqlConnect();
            CommandHandlers = new Dictionary<string, Func<int,string, string, string[], int, Task>>(StringComparer.OrdinalIgnoreCase);

            // Register the bot in the system if it doesn't exist already
            RegisterBot();

            // Register basic commands that all bots should have
            RegisterCommonCommands();
        }

        /// <summary>
        /// Registers the bot in the system database if it doesn't exist already
        /// </summary>
        private void RegisterBot()
        {
            // Check if bot already exists
            if (SqlConnect.IsExist(BotUsername))
            {
                // Bot exists, get its user ID
                BotUserId = SqlConnect.GetUserIdByUsername(BotUsername);
            }
            else
            {
                // Bot doesn't exist, create it
                byte[] botAvatar = GetDefaultBotAvatar();

                // Insert bot as a special user with role 3 (bot)
                BotUserId = SqlConnect.InsertNewBot(
                    BotUsername,
                    "bot_password_not_used",
                    GetBotType(),
                    "Bot",
                    "bot@system.local",
                    "Server",
                    "Other",
                    botAvatar,
                    3); // Role 3 for bots
            }
        }

        /// <summary>
        /// Gets the bot type (e.g., "ModBot", "TranslateBot")
        /// </summary>
        protected abstract string GetBotType();

        /// <summary>
        /// Gets the default avatar for the bot
        /// </summary>
        protected abstract byte[] GetDefaultBotAvatar();

        /// <summary>
        /// Registers common commands that all bots should respond to
        /// </summary>
        private void RegisterCommonCommands()
        {
            // Help command
            CommandHandlers["help"] = async (userId, username, originalMessage, args, chatRoomId) =>
            {
                SendHelpMessage(chatRoomId, userId, username, originalMessage);
            };

            // About command
            CommandHandlers["about"] = async (userId, username, originalMessage, args, chatRoomId) =>
            {
                SendMessage(chatRoomId, userId, username, originalMessage, $"I am {BotUsername}, a {GetBotType()} for this Discord server.");
            };

            // Register bot-specific commands
            RegisterCommands();
        }

        /// <summary>
        /// Registers bot-specific commands
        /// </summary>
        protected abstract void RegisterCommands();

        /// <summary>
        /// Sends a help message listing all available commands
        /// </summary>
        protected abstract void SendHelpMessage(int chatRoomId, int userId, string username, string originalMessage);

        

        
        /// <summary>
        /// Process a message to check if it's a command for this bot
        /// </summary>
        /// <param name="userId">User ID who sent the message</param>
        /// <param name="username">Username who sent the message</param>
        /// <param name="message">Message content</param>
        /// <param name="chatRoomId">Chat room ID where the message was sent</param>
        /// <returns>True if the message was processed as a command, false otherwise</returns>
        public virtual async Task<bool> ProcessMessage(int userId, string username, string message, int chatRoomId)
        {
            // Check if the message starts with the command prefix
            if (message.StartsWith(CommandPrefix))
            {
                // Extract the command and arguments
                string[] parts = message.Substring(CommandPrefix.Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 0)
                {
                    string command = parts[0].ToLower();
                    string[] args = parts.Length > 1 ? parts.Skip(1).ToArray() : new string[0];

                    // Check if this is a registered command
                    if (CommandHandlers.ContainsKey(command))
                    {
                        try
                        {
                            await CommandHandlers[command](userId, username, message, args, chatRoomId);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error executing command {command}: {ex.Message}");
                            SendMessage(chatRoomId, userId, username, message, $"Error executing command: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Unknown command
                        SendMessage(chatRoomId, userId, username, message, $"Unknown command '{command}'. Type {CommandPrefix}help for a list of commands.");
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sends a message to a specific chat room
        /// </summary>
        /// <param name="chatRoomId">Chat room ID to send the message to</param>
        /// <param name="message">Message content</param>
        protected void SendMessage(int chatRoomId, int userId, string username, string originalMessage, string botMessage)
        {
            RoomsManager.StoreAndSendMessageToAllUsersButOne(userId, username, originalMessage, chatRoomId); // send the user's message
            RoomsManager.StoreAndSendMessageToAllUsersButOne(this.BotUserId, this.BotUsername, botMessage, chatRoomId); // send the bot's respone
        }

        protected void SendMessageOnlyFromBot(int chatRoomId, string botMessage)
        {
            RoomsManager.StoreAndSendMessageToAllUsersButOne(this.BotUserId, this.BotUsername, botMessage, chatRoomId); // send the bot's respone
        }



    }
}
