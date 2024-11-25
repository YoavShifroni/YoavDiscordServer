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
        public static Hashtable AllClients = new Hashtable();

        private TcpClient _client;

        private string _clientIP;

        private byte[] _data;

        private DateTime _lastConnect;

        private bool _isFirstMessage = true;

        private CommandHandlerForSingleUser _commandHandlerForSingleUser;

        private int messageLength = -1;

        private int totalBytesRead = 0;
        
        private MemoryStream memoryStream = new MemoryStream();


        public DiscordClientConnection(TcpClient client)
        {
            int count = 0;
            this._client = client;

            // get the ip address of the client to register him with our client list
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
                throw new Exception("someone trying to connect too fast (DOS)");
            }

            // Add the new client to our clients collection
            AllClients.Add(this._clientIP, this);

            // Read data from the client async
            this._data = new byte[this._client.ReceiveBufferSize];

            this._lastConnect = DateTime.Now;

            this._commandHandlerForSingleUser = new CommandHandlerForSingleUser(this);

            // BeginRead will begin async read from the NetworkStream
            // This allows the server to remain responsive and continue accepting new connections from other clients
            // When reading complete control will be transfered to the ReviveMessage function.

            _client.GetStream().BeginRead(this._data,
                                          0,
                                          System.Convert.ToInt32(this._client.ReceiveBufferSize),
                                          ReceiveMessage,
                                          _client.GetStream());


        }

        /// <summary>
        /// The function convert the string that we want to send to the server into byte array and send it to the server over the tcp connection
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendMessage(string message)
        {
            Console.WriteLine(message);
            try
            {
                NetworkStream ns;

                // we use lock to present multiple threads from using the networkstream object
                // this is likely to occur when the server is connected to multiple clients all of 
                // them trying to access to the networkstream at the same time.
                lock (this._client.GetStream())
                {
                    ns = this._client.GetStream();
                }

                if(this._isFirstMessage)
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
        /// The function reads bytes from the socket. When this is a new message, the first 4 bytes represent the length
        /// of the message. This is needed because TCP has max of 64K size, so if we need to send something bigger than 64K,
        /// we need do something special.
        /// This message will continue to read from the socket, until we receive the full message (until we read number of bytes, that is
        /// equals to the 'length'.
        /// Inspired by chat GPT 
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
                            // Move the memory steam's read pointer to the beginning 
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

                        //Reset all the properties that read from the socket, so we are ready for the next message
                        this.messageLength = -1;
                        this.totalBytesRead = 0;
                        this.memoryStream.SetLength(0);
                    }
                }
                lock (this._client.GetStream())
                {
                    // continue reading form the client
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
        /// The function processes the message that we received from the socket (the full message)
        /// </summary>
        /// <param name="messageData"></param>
        /// <param name="bytesRead"></param>
        public void ProcessMessage(byte[] messageData, int bytesRead)
        {     
            string commandRecive = System.Text.Encoding.UTF8.GetString(messageData, 0, bytesRead);
            Console.WriteLine("commandRecive: "+ commandRecive);

            if (this._isFirstMessage)
            {
                RsaFunctions.PublicKey = JsonConvert.DeserializeObject<RSAParameters>(commandRecive);
                Console.WriteLine("Rsa public key recived");
                var jsonString = JsonConvert.SerializeObject(AesFunctions.AesKeys);
                this.SendMessage(jsonString);
                Console.WriteLine("Aes key and iv send");
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
        /// Close down the connection
        /// </summary>
        public void Close()
        {
            AllClients.Remove(this._clientIP);
            this._client.Close();
        }
    }
}
