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
        /// The IP address of the connected client.
        /// </summary>
        private string _clientIP;

        /// <summary>
        /// Timestamp of the client's last connection.
        /// </summary>
        private DateTime _lastConnect;

        

        /// <summary>
        /// Handler for processing commands for a single user.
        /// </summary>
        private CommandHandlerForSingleUser _commandHandlerForSingleUser;

        private TcpConnectionHandler _tcpConnectionHandler;

        

        /// <summary>
        /// Constructor with parameter
        /// </summary>
        /// <param name="client">The TCP client representing the connection.</param>
        public DiscordClientConnection(TcpClient client)
        {
            int count = 0;

            // Get the IP address of the client to register in our client list
            this._clientIP = client.Client.RemoteEndPoint.ToString();

            // DOS protection
            foreach (DictionaryEntry user in AllClients)
            {
                string ipAndPort = (string)(user.Key);
                int index = ipAndPort.LastIndexOf(':');
                string ip = ipAndPort.Substring(0, index);
                IPAddress newIp = IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
                if (ip.Equals(newIp.ToString()))
                {
                    DateTime time = ((DiscordClientConnection)user.Value)._lastConnect;
                    if ((DateTime.Now - time).TotalSeconds < 10)
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

            this._tcpConnectionHandler = new TcpConnectionHandler(client, this);


            this._commandHandlerForSingleUser = new CommandHandlerForSingleUser(this);
            
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



        /// <summary>
        /// Processes a received message from the client.
        /// </summary>
        /// <param name="messageData">The raw message data.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        public void ProcessMessage(byte[] messageData, int bytesRead, bool isFirstMessage)
        {
            string commandRecive = System.Text.Encoding.UTF8.GetString(messageData, 0, bytesRead);
            Console.WriteLine("commandRecive: " + commandRecive);

            if (isFirstMessage)
            {
                RsaFunctions.PublicKey = JsonConvert.DeserializeObject<RSAParameters>(commandRecive);
                Console.WriteLine("Rsa public key recived");
                var jsonString = JsonConvert.SerializeObject(AesFunctions.AesKeys);
                this.SendMessage(jsonString);
                Console.WriteLine("Aes key and iv sent");

            }
            else
            {
                commandRecive = AesFunctions.Decrypt(commandRecive);
                string[] stringSeparators = new string[] { "\r\n" };
                string[] lines = commandRecive.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    Console.WriteLine(lines[i]);
                    this._commandHandlerForSingleUser.HandleCommand(lines[i]);
                }
            }
        }

        /// <summary>
        /// Closes the client connection and removes it from the list of active clients.
        /// </summary>
        public void Close()
        {
            AllClients.Remove(this._clientIP);
            this._tcpConnectionHandler.Close();
        }

        public void CleanUpConnection()
        {
            AllClients.Remove(this._clientIP);
        }
    }
}
