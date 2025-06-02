using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public class ModerationBot : Bot
    {
        // List of banned words for message filtering
        private List<string> _bannedWords;

        // Welcome channel ID (default to first text channel)
        private int _welcomeChannelId = 1;

        // Welcome messages
        private List<string> _welcomeMessages;

        /// <summary>
        /// Constructor
        /// </summary>
        public ModerationBot() : base("ModBot", "!")
        {
            _bannedWords = new List<string> {   
                "fuck",
                "curse",
                "damn",
                "hell"
            };

            _welcomeMessages = new List<string>
            {
                "Welcome to the server, {username}! 👋 Enjoy your stay!",
                "Hello {username}! Welcome to our community! 🎉",
                "Everyone, please welcome {username} to the server! 🥳",
                "A new member has arrived! Welcome, {username}! 🚀",
                "{username} has joined our ranks! Welcome! 🌟"
            };

            
        }

        /// <summary>
        /// Gets the bot type
        /// </summary>
        protected override string GetBotType()
        {
            return "ModBot";
        }

        /// <summary>
        /// Gets the default avatar for the bot
        /// </summary>
        protected override byte[] GetDefaultBotAvatar()
        {
            try
            {
                // Create a shield-shaped avatar with "MOD" text for the moderation bot
                using (Bitmap botAvatar = new Bitmap(200, 200))
                {
                    using (Graphics g = Graphics.FromImage(botAvatar))
                    {
                        // Fill background with a blue color
                        g.Clear(Color.FromArgb(65, 105, 225)); // Royal Blue

                        // Draw shield shape
                        Point[] shieldPoints = {
                            new Point(100, 20),    // Top point
                            new Point(180, 60),    // Top right
                            new Point(160, 160),   // Bottom right
                            new Point(100, 180),   // Bottom point
                            new Point(40, 160),    // Bottom left
                            new Point(20, 60)      // Top left
                        };
                        g.FillPolygon(new SolidBrush(Color.FromArgb(30, 70, 150)), shieldPoints);
                        g.DrawPolygon(new Pen(Color.White, 3), shieldPoints);

                        // Draw MOD text
                        using (Font font = new Font("Arial", 36, FontStyle.Bold))
                        {
                            StringFormat sf = new StringFormat();
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            g.DrawString("MOD", font, Brushes.White, new RectangleF(0, 0, 200, 200), sf);
                        }
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        botAvatar.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating bot avatar: {ex.Message}");
                // Return a default empty byte array if there's an error
                return new byte[0];
            }
        }

        /// <summary>
        /// Register bot-specific commands
        /// </summary>
        protected override void RegisterCommands()
        {
            // Rules command
            CommandHandlers["rules"] = async (userId, username, originalMessage, args, chatRoomId) =>
            {
                SendServerRules(chatRoomId, userId, username, originalMessage);
            };

            // Add banned word command
            CommandHandlers["banword"] = async (userId, username, originalMessage, args, chatRoomId) =>
            {
                // Check if user has permission (admin or moderator)
                int userRole = SqlConnect.GetUserRole(userId);
                if (userRole > 1) // Not admin (0) or moderator (1)
                {
                    SendMessage(chatRoomId,userId, username, originalMessage, "You don't have permission to add banned words.");
                    return;
                }

                if (args.Length > 0)
                {
                    AddBannedWord(args[0]);
                    SendMessage(chatRoomId, userId, username, originalMessage, $"Added '{args[0]}' to the banned words list.");
                }
                else
                {
                    SendMessage(chatRoomId, userId, username, originalMessage, "Please specify a word to ban.");
                }
            };

            // Remove banned word command
            CommandHandlers["unbanword"] = async (userId, username, originalMessage, args, chatRoomId) =>
            {
                // Check if user has permission (admin or moderator)
                int userRole = SqlConnect.GetUserRole(userId);
                if (userRole > 1) // Not admin (0) or moderator (1)
                {
                    SendMessage(chatRoomId, userId, username, originalMessage, "You don't have permission to remove banned words.");
                    return;
                }

                if (args.Length > 0)
                {
                    RemoveBannedWord(args[0]);
                    SendMessage(chatRoomId, userId, username, originalMessage, $"Removed '{args[0]}' from the banned words list.");
                }
                else
                {
                    SendMessage(chatRoomId, userId, username, originalMessage, "Please specify a word to unban.");
                }
            };

            // List banned words command
            CommandHandlers["bannedwords"] = async (userId, username, originalMessage, args, chatRoomId) =>
            {
                SendMessage(chatRoomId, userId, username, originalMessage, $"Banned words: {string.Join(", ", _bannedWords)}");
            };
        }

        /// <summary>
        /// Sends help message with available commands
        /// </summary>
        protected override void SendHelpMessage(int chatRoomId, int userId, string username, string originalMessage)
        {
            string helpMessage = "**Moderation Bot Commands:**\n" +
                                 $"{CommandPrefix}help - Shows this help message\n" +
                                 $"{CommandPrefix}rules - Shows the server rules\n" +
                                 $"{CommandPrefix}banword <word> - Adds a word to the banned list (admin/mod only)\n" +
                                 $"{CommandPrefix}unbanword <word> - Removes a word from the banned list (admin/mod only)\n" +
                                 $"{CommandPrefix}bannedwords - Lists all banned words\n" +
                                 $"{CommandPrefix}about - Shows information about this bot";

            SendMessage(chatRoomId, userId, username, originalMessage, helpMessage);
        }

        /// <summary>
        /// Process a message for moderation
        /// </summary>
        public override async Task<bool> ProcessMessage(int userId, string username, string message, int chatRoomId)
        {
            // First, check if this is a command for this bot
            bool handled = await base.ProcessMessage(userId, username, message, chatRoomId);
            if (handled)
                return true;

            // If not a command, check for forbidden words
            if (ContainsBannedWord(message, out string bannedWord))
            {
                // Send private message to the user who sent the banned word
                string warningMessage = $"@{username}, your message containing the banned word '{bannedWord}' was not sent. Please watch your language.";
                ClientServerProtocol warningProtocol = new ClientServerProtocol();
                warningProtocol.TypeOfCommand = TypeOfCommand.Message_From_Other_User_Command;
                warningProtocol.Username = this.BotUsername;
                warningProtocol.UserId = this.BotUserId;
                warningProtocol.MessageThatTheUserSent = warningMessage;
                warningProtocol.TimeThatTheMessageWasSent = DateTime.UtcNow;
                warningProtocol.ChatRoomId = chatRoomId;
                DiscordClientConnection.SendMessageToSpecificUser(userId, warningProtocol);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Called when a new user registers
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="username">Username</param>
        public void OnUserRegistered(int userId, string username)
        {
            // Send a welcome message to the welcome channel
            string welcomeMessage = GetRandomWelcomeMessage(username);
            SendMessageOnlyFromBot(_welcomeChannelId, welcomeMessage);
        }

        /// <summary>
        /// Sends the server rules to a specific chat room
        /// </summary>
        /// <param name="chatRoomId">Chat room ID</param>
        private void SendServerRules(int chatRoomId, int userId, string username, string originalMessage)
        {
            string rules = "**Server Rules:**\n" +
                           "1. Be respectful to all members\n" +
                           "2. No offensive language or harassment\n" +
                           "3. No spamming or flooding the chat\n" +
                           "4. Keep discussions in the appropriate channels\n" +
                           "5. Follow the administrators' and moderators' instructions";

            SendMessage(chatRoomId, userId, username, originalMessage, rules);
        }

        /// <summary>
        /// Checks if a message contains any banned words
        /// </summary>
        /// <param name="message">Message to check</param>
        /// <param name="bannedWord">The banned word that was found</param>
        /// <returns>True if a banned word was found, false otherwise</returns>
        private bool ContainsBannedWord(string message, out string bannedWord)
        {
            string lowerMessage = message.ToLower();
            foreach (string word in _bannedWords)
            {
                if (Regex.IsMatch(lowerMessage, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase))
                {
                    bannedWord = word;
                    return true;
                }
            }

            bannedWord = null;
            return false;
        }

        /// <summary>
        /// Adds a word to the banned words list
        /// </summary>
        /// <param name="word">Word to ban</param>
        private void AddBannedWord(string word)
        {
            if (!_bannedWords.Contains(word.ToLower()))
            {
                _bannedWords.Add(word.ToLower());
            }
        }

        /// <summary>
        /// Removes a word from the banned words list
        /// </summary>
        /// <param name="word">Word to unban</param>
        private void RemoveBannedWord(string word)
        {
            _bannedWords.Remove(word.ToLower());
        }

        /// <summary>
        /// Gets a random welcome message
        /// </summary>
        /// <param name="username">Username to include in the message</param>
        /// <returns>Welcome message with username inserted</returns>
        private string GetRandomWelcomeMessage(string username)
        {
            Random rand = new Random();
            int index = rand.Next(_welcomeMessages.Count);
            return _welcomeMessages[index].Replace("{username}", username);
        }
    }
}
