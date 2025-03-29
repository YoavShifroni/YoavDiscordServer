using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YoavDiscordServer
{
    public class TranslateBot : Bot
    {
        // Dictionary of supported languages with their codes
        private Dictionary<string, string> _supportedLanguages;

        // API key for Google Translate API (replace with your own)
        private const string API_KEY = "YOUR_GOOGLE_TRANSLATE_API_KEY";

        // HttpClient for API calls
        private HttpClient _httpClient;

        /// <summary>
        /// Constructor
        /// </summary>
        public TranslateBot() : base("TranslateBot", "/")
        {
            _supportedLanguages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "english", "en" },
                { "spanish", "es" },
                { "french", "fr" },
                { "german", "de" },
                { "italian", "it" },
                { "portuguese", "pt" },
                { "russian", "ru" },
                { "japanese", "ja" },
                { "korean", "ko" },
                { "chinese", "zh" },
                { "arabic", "ar" },
                { "hindi", "hi" },
                { "dutch", "nl" },
                { "greek", "el" },
                { "hebrew", "he" },
                { "polish", "pl" },
                { "turkish", "tr" },
                { "swedish", "sv" },
                { "danish", "da" },
                { "finnish", "fi" },
                { "norwegian", "no" },
                { "czech", "cs" },
                { "hungarian", "hu" },
                { "romanian", "ro" },
                { "thai", "th" },
                { "vietnamese", "vi" },
                { "indonesian", "id" },
                { "malay", "ms" }
            };

            

            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Gets the bot type
        /// </summary>
        protected override string GetBotType()
        {
            return "TranslateBot";
        }

        /// <summary>
        /// Gets the default avatar for the bot
        /// </summary>
        protected override byte[] GetDefaultBotAvatar()
        {
            try
            {
                // Create an avatar with a globe icon for the translation bot
                using (Bitmap botAvatar = new Bitmap(200, 200))
                {
                    using (Graphics g = Graphics.FromImage(botAvatar))
                    {
                        // Fill background with a teal color
                        g.Clear(Color.FromArgb(0, 128, 128)); // Teal

                        // Draw a circle for the globe
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using (var brush = new SolidBrush(Color.FromArgb(173, 216, 230))) // Light blue
                        {
                            g.FillEllipse(brush, 50, 50, 100, 100);
                        }

                        // Draw latitude lines
                        using (var pen = new Pen(Color.FromArgb(0, 64, 64), 2))
                        {
                            g.DrawLine(pen, 50, 100, 150, 100); // Equator
                            g.DrawLine(pen, 60, 70, 140, 70); // Upper parallel
                            g.DrawLine(pen, 60, 130, 140, 130); // Lower parallel
                        }

                        // Draw longitude lines
                        using (var pen = new Pen(Color.FromArgb(0, 64, 64), 2))
                        {
                            g.DrawLine(pen, 100, 50, 100, 150); // Prime meridian
                            g.DrawArc(pen, 50, 50, 100, 100, 150, 60); // Left curve
                            g.DrawArc(pen, 50, 50, 100, 100, 330, 60); // Right curve
                        }

                        // Draw "T" for Translate
                        using (Font font = new Font("Arial", 36, FontStyle.Bold))
                        {
                            StringFormat sf = new StringFormat();
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;
                            g.DrawString("T", font, Brushes.White, new RectangleF(0, 0, 200, 200), sf);
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
            // Translate command
            CommandHandlers["translate"] = async (userId, username, originalMessage, args, chatRoomId) =>
            {
                if (args.Length == 0)
                {
                    SendMessage(chatRoomId, userId, username, originalMessage, $"Usage: {CommandPrefix}translate [source language] [target language] [text]\n" +
                        $"or: {CommandPrefix}translate [target language] [text] (uses auto-detection for source language)\n" +
                        $"Use {CommandPrefix}languages to see supported languages.");
                    return;
                }

                string sourceLanguage = "auto";
                string targetLanguage;
                string textToTranslate;

                if (args.Length >= 3 && IsLanguageSupported(args[0]) && IsLanguageSupported(args[1]))
                {
                    // Format: #translate english spanish Hello, how are you?
                    sourceLanguage = GetLanguageCode(args[0]);
                    targetLanguage = GetLanguageCode(args[1]);
                    textToTranslate = string.Join(" ", args.Skip(2));
                }
                else if (args.Length >= 2 && IsLanguageSupported(args[0]))
                {
                    // Format: #translate spanish Hello, how are you?
                    targetLanguage = GetLanguageCode(args[0]);
                    textToTranslate = string.Join(" ", args.Skip(1));
                }
                else
                {
                    
                    SendMessage(chatRoomId, userId, username, originalMessage, $"Please specify a target language. Use {CommandPrefix}languages to see supported languages.");
                    return;
                }

                

                // Perform the translation
                try
                {
                    string translatedText = await TranslateText(textToTranslate, sourceLanguage, targetLanguage);
                    string sourceLangDisplay = sourceLanguage == "auto" ? "Detected language" : GetLanguageName(sourceLanguage);
                    string targetLangDisplay = GetLanguageName(targetLanguage);
                    SendMessage(chatRoomId, userId, username, originalMessage, $"📝 {sourceLangDisplay} ➡️ {targetLangDisplay}:\n" +
                                               $"**Original**: {textToTranslate}\n" +
                                               $"**Translation**: {translatedText}");
                }
                catch (Exception ex)
                {
                    SendMessage(chatRoomId, userId, username, originalMessage, $"Error during translation: {ex.Message}");
                }
            };

            // Languages command
            CommandHandlers["languages"] = async (userId, username, originalMessage, args, chatRoomId) =>
            {
                string languageList = "**Supported Languages**:\n" +
                    string.Join(", ", _supportedLanguages.Keys.OrderBy(k => k));
                SendMessage(chatRoomId, userId, username, originalMessage, languageList);
            };

            // Detect language command
            CommandHandlers["detect"] = async (userId, username, originalMessage, args, chatRoomId) =>
            {
                if (args.Length < 1)
                {
                    SendMessage(chatRoomId, userId, username, originalMessage, $"Usage: {CommandPrefix}detect [text] - Detects the language of the provided text.");
                    return;
                }

                string textToDetect = string.Join(" ", args);

                try
                {
                    string detectedLanguage = await DetectLanguage(textToDetect);
                    string langName = GetLanguageName(detectedLanguage);

                    SendMessage(chatRoomId, userId, username, originalMessage, $"🔍 Language detection: The text appears to be in **{langName}** ({detectedLanguage}).");
                }
                catch (Exception ex)
                {
                    SendMessage(chatRoomId, userId, username, originalMessage, $"Error during language detection: {ex.Message}");
                }
            };
        }

        /// <summary>
        /// Sends help message with available commands
        /// </summary>
        protected override void SendHelpMessage(int chatRoomId, int userId, string username, string originalMessage)
        {
            string helpMessage = "**Translation Bot Commands:**\n" +
                                 $"{CommandPrefix}translate [source language] [target language] [text] - Translates text between languages\n" +
                                 $"{CommandPrefix}translate [target language] [text] - Translates text to target language (auto-detects source)\n" +
                                 $"{CommandPrefix}autotranslate [text] - Translates text to your preferred language\n" +
                                 $"{CommandPrefix}setlang [language] - Sets your preferred language for translations\n" +
                                 $"{CommandPrefix}detect [text] - Detects the language of the provided text\n" +
                                 $"{CommandPrefix}languages - Shows a list of supported languages\n" +
                                 $"{CommandPrefix}help - Shows this help message\n" +
                                 $"{CommandPrefix}about - Shows information about this bot";

            SendMessage(chatRoomId, userId, username, originalMessage, helpMessage);
        }

        /// <summary>
        /// Process a message and handle it if relevant for translation
        /// </summary>
        public override async Task<bool> ProcessMessage(int userId, string username, string message, int chatRoomId)
        {
            // Check if this is a command for this bot
            if (await base.ProcessMessage(userId, username, message, chatRoomId))
                return true;

            // Check if the message is asking about translation
            if (message.ToLower().Contains("translate") || message.ToLower().Contains("translation") ||
                message.ToLower().Contains("language") || message.ToLower().Contains("interpret"))
            {
                SendMessage(chatRoomId,userId, username, message,  $"Hi {username}! I can help with translations. Use `{CommandPrefix}help` " +
                    $"to see available commands or try `{CommandPrefix}translate [target language] [text]` to translate text.");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a language is supported
        /// </summary>
        /// <param name="languageName">Language name to check</param>
        /// <returns>True if the language is supported</returns>
        private bool IsLanguageSupported(string languageName)
        {
            return _supportedLanguages.ContainsKey(languageName) || _supportedLanguages.ContainsValue(languageName.ToLower());
        }

        /// <summary>
        /// Gets the language code for a language name
        /// </summary>
        /// <param name="languageName">Language name</param>
        /// <returns>Language code</returns>
        private string GetLanguageCode(string languageName)
        {
            // If it's already a code, return it
            if (_supportedLanguages.ContainsValue(languageName.ToLower()))
                return languageName.ToLower();

            // Otherwise, lookup the code
            if (_supportedLanguages.TryGetValue(languageName, out string code))
                return code;

            return languageName.ToLower(); // Fallback
        }

        /// <summary>
        /// Gets the language name for a language code
        /// </summary>
        /// <param name="languageCode">Language code</param>
        /// <returns>Language name</returns>
        private string GetLanguageName(string languageCode)
        {
            foreach (var pair in _supportedLanguages)
            {
                if (pair.Value.Equals(languageCode, StringComparison.OrdinalIgnoreCase))
                    return pair.Key;
            }

            return languageCode; // Fallback
        }

        /// <summary>
        /// Translates text using Google Translate API
        /// </summary>
        /// <param name="text">Text to translate</param>
        /// <param name="sourceLanguage">Source language code</param>
        /// <param name="targetLanguage">Target language code</param>
        /// <returns>Translated text</returns>
        private async Task<string> TranslateText(string text, string sourceLanguage, string targetLanguage)
        {
            try
            {

                // Actual Google Translate API implementation
                string url = $"https://translation.googleapis.com/language/translate/v2?key={API_KEY}";

                var content = new Dictionary<string, string>
                {
                    { "q", text },
                    { "target", targetLanguage }
                };

                if (sourceLanguage != "auto")
                {
                    content.Add("source", sourceLanguage);
                }

                var httpContent = new FormUrlEncodedContent(content);
                var response = await _httpClient.PostAsync(url, httpContent);

                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic data = JObject.Parse(jsonResponse);

                return data.data.translations[0].translatedText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Translation error: {ex.Message}");
                throw new Exception("Failed to translate text. Please try again later.");
            }
        }

        /// <summary>
        /// Detects the language of text using Google Translate API
        /// </summary>
        /// <param name="text">Text to detect language for</param>
        /// <returns>Detected language code</returns>
        private async Task<string> DetectLanguage(string text)
        {
            try
            {
                
                // Actual Google Translate API implementation
                string url = $"https://translation.googleapis.com/language/translate/v2/detect?key={API_KEY}";

                var content = new Dictionary<string, string>
                {
                    { "q", text }
                };

                var httpContent = new FormUrlEncodedContent(content);
                var response = await _httpClient.PostAsync(url, httpContent);

                response.EnsureSuccessStatusCode();

                string jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic data = JObject.Parse(jsonResponse);

                return data.data.detections[0][0].language;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Detection error: {ex.Message}");
                throw new Exception("Failed to detect language. Please try again later.");
            }
        }


        
    }
}