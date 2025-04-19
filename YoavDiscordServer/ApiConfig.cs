using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public static class ApiConfig
    {
        /// <summary>
        /// The config file path - const field
        /// </summary>
        private const string CONFIG_FILE_PATH = "AppSettings.json";

        /// <summary>
        ///  Retrieve the API key from environment variables or JSON settings
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public static string GetGoogleApiKey()
        {
            string apiKey =  ReadApiKeyFromJsonFile();

            // Provide a fallback message for developers if key is not found
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ApplicationException("Google API key not found. Please set the GOOGLE_API_KEY environment variable or update your appsettings.json file.");
            }

            return apiKey;
        }

        /// <summary>
        /// Read the API key
        /// </summary>
        /// <returns></returns>
        private static string ReadApiKeyFromJsonFile()
        {
            try
            {
                // Check if config file exists
                if (!File.Exists(CONFIG_FILE_PATH))
                {
                    return null;
                }

                // Read and parse the JSON file
                string jsonContent = File.ReadAllText(CONFIG_FILE_PATH);

                // Parse the JSON content
                JObject jsonObject = JObject.Parse(jsonContent);

                // Try to get the GoogleApiKey property
                JToken apiKeyToken = jsonObject["GoogleApiKey"];

                // Check if the token exists and return its value
                if (apiKeyToken != null)
                {
                    return apiKeyToken.ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as appropriate
                Console.WriteLine($"Error reading API key from config file: {ex.Message}");
                return null;
            }
        }
    }
}
