using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
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

        // API key for Google Translate API
        private readonly string API_KEY = ApiConfig.GetGoogleApiKey();

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
        /// Gets the default avatar for the translation bot
        /// </summary>
        protected override byte[] GetDefaultBotAvatar()
        {
            try
            {
                // Create an avatar for the translation bot
                using (Bitmap botAvatar = new Bitmap(200, 200))
                {
                    using (Graphics g = Graphics.FromImage(botAvatar))
                    {
                        // Set high quality rendering
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                        // Fill background with a gradient from blue to purple (translation colors)
                        using (LinearGradientBrush gradientBrush = new LinearGradientBrush(
                            new Rectangle(0, 0, 200, 200),
                            Color.FromArgb(41, 128, 185), // Blue
                            Color.FromArgb(142, 68, 173), // Purple
                            45f))
                        {
                            g.FillRectangle(gradientBrush, 0, 0, 200, 200);
                        }

                        // Draw circular background for the icon
                        using (var circleBrush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                        {
                            g.FillEllipse(circleBrush, 25, 25, 150, 150);
                        }

                        // Draw speech bubbles representing translation
                        // First speech bubble (left-to-right languages)
                        using (var leftBubbleBrush = new SolidBrush(Color.FromArgb(52, 152, 219))) // Blue
                        {
                            // Draw bubble shape
                            g.FillEllipse(leftBubbleBrush, 40, 60, 70, 50);

                            // Draw tail of speech bubble
                            Point[] tailPoints = new Point[] {
                        new Point(50, 110),
                        new Point(40, 125),
                        new Point(65, 105)
                    };
                            g.FillPolygon(leftBubbleBrush, tailPoints);
                        }

                        // Second speech bubble (right-to-left languages)
                        using (var rightBubbleBrush = new SolidBrush(Color.FromArgb(155, 89, 182))) // Purple
                        {
                            // Draw bubble shape
                            g.FillEllipse(rightBubbleBrush, 90, 80, 70, 50);

                            // Draw tail of speech bubble
                            Point[] tailPoints = new Point[] {
                        new Point(150, 130),
                        new Point(160, 145),
                        new Point(135, 125)
                    };
                            g.FillPolygon(rightBubbleBrush, tailPoints);
                        }

                        // Draw text symbols in the speech bubbles
                        using (Font fontA = new Font("Arial", 14, FontStyle.Bold))
                        using (Font fontB = new Font("Arial", 14, FontStyle.Bold))
                        {
                            StringFormat sf = new StringFormat();
                            sf.Alignment = StringAlignment.Center;
                            sf.LineAlignment = StringAlignment.Center;

                            // Left bubble text (can be "A" or source language text)
                            g.DrawString("A", fontA, Brushes.White, new RectangleF(40, 57, 70, 50), sf);

                            // Right bubble text (can be "B" or target language text)
                            g.DrawString("B", fontB, Brushes.White, new RectangleF(90, 77, 70, 50), sf);
                        }

                        // Draw arrows between the bubbles to indicate translation
                        using (var arrowPen = new Pen(Color.White, 2.5f))
                        {
                            // Set the arrow cap
                            arrowPen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;

                            // Draw arrow from left to right
                            g.DrawLine(arrowPen, 75, 80, 100, 95);

                            // Reverse the arrow direction
                            arrowPen.StartCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
                            arrowPen.EndCap = System.Drawing.Drawing2D.LineCap.NoAnchor;

                            // Draw arrow from right to left
                            g.DrawLine(arrowPen, 100, 105, 75, 90);
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
            {
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

                string translatedText = data.data.translations[0].translatedText;

                return WebUtility.HtmlDecode(translatedText);
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

                string detectedCode = data.data.detections[0][0].language;

                // Normalize deprecated codes
                if (detectedCode == "iw") detectedCode = "he";
                else if (detectedCode == "in") detectedCode = "id";
                else if (detectedCode == "ji") detectedCode = "yi";
                else if (detectedCode == "zh-CN") detectedCode = "zh";

                return detectedCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Detection error: {ex.Message}");
                throw new Exception("Failed to detect language. Please try again later.");
            }
        }


        
    }
}