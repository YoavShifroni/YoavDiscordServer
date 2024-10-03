using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
                                          null);


        }

        /// <summary>
        /// Allow the server to send message to the client.
        /// </summary>
        /// <param name="message">Message to send</param>
        public void SendMessage(string message)
        {
            Console.WriteLine(message);
            try
            {
                System.Net.Sockets.NetworkStream ns;

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

        public void ReceiveMessage(IAsyncResult ar)
        {
            int bytesRead;
            try
            {
                lock (this._client.GetStream())
                {
                    // call EndRead to handle the end of an async read.
                    bytesRead = this._client.GetStream().EndRead(ar);
                }
                if (bytesRead < 1) // client was disconnected
                {
                    AllClients.Remove(this._clientIP);
                    return;
                }
                
                string commandRecive = System.Text.Encoding.UTF8.GetString(this._data, 0, bytesRead);
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
                lock (this._client.GetStream())
                {
                    // continue reading form the client
                    this._client.GetStream().BeginRead(this._data, 0, System.Convert.ToInt32(this._client.ReceiveBufferSize),
                        ReceiveMessage, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                AllClients.Remove(this._clientIP);
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
