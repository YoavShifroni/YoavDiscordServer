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
        /// The TCP client representing the connection.
        /// </summary>
        private TcpClient _client;

        /// <summary>
        /// The IP address of the connected client.
        /// </summary>
        private string _clientIP;

        /// <summary>
        /// Buffer for reading data from the client.
        /// </summary>
        private byte[] _data;

        /// <summary>
        /// Timestamp of the client's last connection.
        /// </summary>
        private DateTime _lastConnect;

        /// <summary>
        /// Indicates if the first message has been received from the client.
        /// </summary>
        private bool _isFirstMessage = true;

        /// <summary>
        /// Handler for processing commands for a single user.
        /// </summary>
        private CommandHandlerForSingleUser _commandHandlerForSingleUser;

        /// <summary>
        /// Length of the current message being read from the client.
        /// </summary>
        private int messageLength = -1;

        /// <summary>
        /// Total number of bytes read from the current message.
        /// </summary>
        private int totalBytesRead = 0;

        /// <summary>
        /// Memory stream used for assembling incoming message data.
        /// </summary>
        private MemoryStream memoryStream = new MemoryStream();

        /// <summary>
        /// Constructor with parameter
        /// </summary>
        /// <param name="client">The TCP client representing the connection.</param>
        public DiscordClientConnection(TcpClient client)
        {
            int count = 0;
            this._client = client;

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

            // Add the new client to our clients collection
            AllClients.Add(this._clientIP, this);

            // Read data from the client asynchronously
            this._data = new byte[this._client.ReceiveBufferSize];

            this._lastConnect = DateTime.Now;

            this._commandHandlerForSingleUser = new CommandHandlerForSingleUser(this);

            // Begin async read from the NetworkStream
            _client.GetStream().BeginRead(this._data,
                                          0,
                                          System.Convert.ToInt32(this._client.ReceiveBufferSize),
                                          ReceiveMessage,
                                          _client.GetStream());
        }

        /// <summary>
        /// Sends a message to the client over the TCP connection.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            Console.WriteLine(message);
            try
            {
                NetworkStream ns;

                // Use lock to prevent multiple threads from accessing the network stream simultaneously
                lock (this._client.GetStream())
                {
                    ns = this._client.GetStream();
                }

                if (this._isFirstMessage)
                {
                    message = RsaFunctions.Encrypt(message);
                }
                else
                {
                    message = AesFunctions.Encrypt(message);
                }

                // Send data to the client
                byte[] bytesToSend = System.Text.Encoding.UTF8.GetBytes(message);
                ns.Write(bytesToSend, 0, bytesToSend.Length);
                ns.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Receives a message from the client asynchronously and processes it when complete.
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveMessage(IAsyncResult ar)
        {
            NetworkStream stream = (NetworkStream)ar.AsyncState;

            try
            {
                // Complete the asynchronous read operation
                int bytesRead = stream.EndRead(ar);
                if (bytesRead > 0)
                {
                    // If message length is not set, read the first 4 bytes for message length
                    if (this.messageLength == -1 && this.totalBytesRead < 4)
                    {
                        int remainingLengthBytes = 4 - this.totalBytesRead;
                        int bytesToCopy = Math.Min(bytesRead, remainingLengthBytes);
                        memoryStream.Write(this._data, 0, bytesToCopy);
                        this.totalBytesRead += bytesToCopy;

                        // Check if we have read the full length header
                        if (this.totalBytesRead >= 4)
                        {
                            // Move the memory stream's read pointer to the beginning 
                            this.memoryStream.Seek(0, SeekOrigin.Begin);
                            byte[] lengthBytes = new byte[4];
                            // Read 4 bytes from the memory stream into lengthBytes
                            this.memoryStream.Read(lengthBytes, 0, 4);
                            this.messageLength = BitConverter.ToInt32(lengthBytes, 0);
                            // Reset memory stream to accumulate the rest of the message
                            this.memoryStream.SetLength(0);
                        }

                        // If there’s more data in this chunk, process it as part of the message body
                        if (bytesRead > bytesToCopy)
                        {
                            this.memoryStream.Write(this._data, bytesToCopy, bytesRead - bytesToCopy);
                            this.totalBytesRead += bytesRead - bytesToCopy;
                        }
                    }
                    else
                    {
                        // Accumulate message data into memory stream
                        this.memoryStream.Write(this._data, 0, bytesRead);
                        this.totalBytesRead += bytesRead;
                    }

                    // If we've accumulated the full message, process it
                    if (this.messageLength > 0 && this.totalBytesRead >= this.messageLength + 4)
                    {
                        ProcessMessage(this.memoryStream.ToArray(), this.totalBytesRead - 4);

                        // Reset properties for the next message
                        this.messageLength = -1;
                        this.totalBytesRead = 0;
                        this.memoryStream.SetLength(0);
                    }
                }
                lock (this._client.GetStream())
                {
                    // Continue reading from the client
                    this._client.GetStream().BeginRead(this._data, 0, System.Convert.ToInt32(this._client.ReceiveBufferSize),
                        ReceiveMessage, _client.GetStream());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                AllClients.Remove(this._clientIP);
                stream.Close();
            }
        }

        /// <summary>
        /// Processes a received message from the client.
        /// </summary>
        /// <param name="messageData">The raw message data.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        public void ProcessMessage(byte[] messageData, int bytesRead)
        {
            string commandRecive = System.Text.Encoding.UTF8.GetString(messageData, 0, bytesRead);
            Console.WriteLine("commandRecive: " + commandRecive);

            if (this._isFirstMessage)
            {
                RsaFunctions.PublicKey = JsonConvert.DeserializeObject<RSAParameters>(commandRecive);
                Console.WriteLine("Rsa public key recived");
                var jsonString = JsonConvert.SerializeObject(AesFunctions.AesKeys);
                this.SendMessage(jsonString);
                Console.WriteLine("Aes key and iv sent");
                this._isFirstMessage = false;

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
            this._client.Close();
        }
    }
}
