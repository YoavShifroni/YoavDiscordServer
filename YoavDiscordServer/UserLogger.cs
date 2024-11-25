using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;


namespace YoavDiscordServer
{
    /// <summary>
    /// Provides logging functionality for users, allowing for a unique log file per user based on their username.
    /// </summary>
    public static class UserLogger
    {
        /// <summary>
        /// The logging factory used to create loggers and manage logging configurations.
        /// </summary>
        private static readonly LogFactory logFactory = LogManager.LogFactory;

        /// <summary>
        /// Static constructor that loads the NLog configuration from the specified configuration file.
        /// </summary>
        static UserLogger()
        {
            // Load NLog configuration from nlog.config
            LogManager.LoadConfiguration("nlog.config");
        }

        /// <summary>
        /// Retrieves an ILogger instance for a specific user, associating the username with the logger to create a unique log context.
        /// </summary>
        /// <param name="username">The username to associate with the logger.</param>
        public static ILogger GetLoggerForUser(string username)
        {
            // Set the "username" variable to create a unique log file per user
            MappedDiagnosticsLogicalContext.Set("username", username);
            return logFactory.GetLogger("UserLogger");
        }
    }

}
