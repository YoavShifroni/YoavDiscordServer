using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public class DiscordClientConnection
    {
        /// <summary>
        /// Collection of all connected clients.
        /// </summary>
        public static Hashtable AllClients = new Hashtable();


        /// <summary>
        /// The IP and port address of the connected client.
        /// </summary>
        private IPEndPoint _clientIP;

        /// <summary>
        /// Timestamp of the client's last connection.
        /// </summary>
        private DateTime _lastConnect;

        

        /// <summary>
        /// Handler for processing commands for a single user.
        /// </summary>
        public CommandHandlerForSingleUser CommandHandlerForSingleUser;

        private TcpConnectionHandler _tcpConnectionHandler;

        private AesFunctions _aesFunctions;

        

        /// <summary>
        /// Constructor with parameter
        /// </summary>
        /// <param name="client">The TCP client representing the connection.</param>
        public DiscordClientConnection(TcpClient client)
        {
            int count = 0;

            // Get the IP address of the client to register in our client list
            this._clientIP = client.Client.RemoteEndPoint as IPEndPoint;

            // DOS protection
            foreach (DictionaryEntry user in AllClients)
            {
                bool sameIpAddress = IPAddress.Equals((IPEndPoint)client.Client.RemoteEndPoint, user.Key); // check if it's the same IP address
                if (sameIpAddress)
                {
                    DateTime time = ((DiscordClientConnection)user.Value)._lastConnect;
                    if ((DateTime.Now - time).TotalSeconds < 10) // check the time difference
                    {
                        count++;
                    }
                }
            }
            if (count >= 10)
            {
                throw new Exception("Someone is trying to connect too fast (DOS)");
            }

            this._lastConnect = DateTime.Now;


            // Add the new client to our clients collection
            AllClients.Add(this._clientIP, this);

            this._aesFunctions = new AesFunctions();

            this._tcpConnectionHandler = new TcpConnectionHandler(client, this, this._aesFunctions);


            this.CommandHandlerForSingleUser = new CommandHandlerForSingleUser(this);
            
            this._tcpConnectionHandler.StartListen();
        }

        /// <summary>
        /// Sends a message to the client over the TCP connection.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            this._tcpConnectionHandler.SendMessage(message);
        }

        public static void Broadcast(string message)
        {
            foreach (DiscordClientConnection user in AllClients.Values)
            {
                user.SendMessage(message);
            }
        }



        /// <summary>
        /// Processes a received message from the client.
        /// </summary>
        /// <param name="messageData">The raw message data.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        public void ProcessMessage(byte[] messageData, int bytesRead, bool isFirstMessage)
        {
            string commandRecive = System.Text.Encoding.UTF8.GetString(messageData, 0, bytesRead);

            if (isFirstMessage)
            {
                RsaFunctions.PublicKey = JsonConvert.DeserializeObject<RSAParameters>(commandRecive);
                Console.WriteLine("Rsa public key recived");
                var jsonString = JsonConvert.SerializeObject(this._aesFunctions.AesKeys);
                this.SendMessage(jsonString);
                Console.WriteLine("Aes key and iv sent");

            }
            else
            {
                commandRecive = this._aesFunctions.Decrypt(commandRecive);
                string[] stringSeparators = new string[] { ClientServerProtocolParser.MessageTrailingDelimiter };
                string[] lines = commandRecive.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    this.CommandHandlerForSingleUser.HandleCommand(lines[i]);
                }
            }
        }

       
        /// <summary>
        /// Cleanup this connection
        /// </summary>
        public void CleanUpConnection()
        {
            try
            {
                AllClients.Remove(this._clientIP);

                this.CommandHandlerForSingleUser.RemoveUserFromAllMediaRooms();
            }
            catch
            {
                // ignore
            }

        }

        /// <summary>
        /// Send message to all users except one
        /// </summary>
        /// <param name="userIdToExclude"></param>
        /// <param name="protocol"></param>
        public static void SendMessageToAllUserExceptOne(int userIdToExclude, ClientServerProtocol protocol)
        {
            Console.WriteLine("Message sent to clients: " + protocol.ToString());
            foreach (DiscordClientConnection user in AllClients.Values)
            {
                if (user.CommandHandlerForSingleUser._userId > 0 && user.CommandHandlerForSingleUser._userId != userIdToExclude && user.CommandHandlerForSingleUser.IsAuthenticated)
                {
                    user.SendMessage(ClientServerProtocolParser.Generate(protocol));
                }
            }
        }

        /// <summary>
        /// Send message to specific user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="protocol"></param>
        public static void SendMessageToSpecificUser(int userId, ClientServerProtocol protocol)
        {
            foreach (DiscordClientConnection user in AllClients.Values)
            {
                if (user.CommandHandlerForSingleUser._userId > 0 && user.CommandHandlerForSingleUser._userId == userId)
                {
                    Console.WriteLine("Message sent to client: " + protocol.ToString());
                    user.SendMessage(ClientServerProtocolParser.Generate(protocol));
                    return;
                }
            }
        }

        /// <summary>
        /// Get user by his id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string GetUserIpById(int userId)
        {
            foreach(DiscordClientConnection user in AllClients.Values)
            {
                if(user.CommandHandlerForSingleUser._userId == userId)
                {
                    return user._clientIP.Address.ToString();
                }
            }
            return null;
        }

        /// <summary>
        /// Get the object DiscordClientConnection by the user id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static DiscordClientConnection GetDiscordClientConnectionById(int userId)
        {
            foreach (DiscordClientConnection user in AllClients.Values)
            {
                if (user.CommandHandlerForSingleUser._userId == userId)
                {
                    return user;
                }
            }
            return null;
        }

        /// <summary>
        /// Get all the ids of connected users
        /// </summary>
        /// <returns></returns>
        public static List<int> GetIdsOfAllConnectedUsers()
        {
            List<int> ids = new List<int>();
            foreach(DiscordClientConnection user in AllClients.Values)
            {
                ids.Add(user.CommandHandlerForSingleUser._userId);
            }
            return ids;
        }

        /// <summary>
        /// Checkk if this user already connected
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool CheckIfUserAlreadyConnected(int userId)
        {
            int count = 0;
            foreach(DiscordClientConnection user in AllClients.Values)
            {
                if(user.CommandHandlerForSingleUser._userId == userId)
                {
                    count++;
                }
            }
            return count >= 2;
        }
    }
}
