﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MongoDB.Driver.WriteConcern;

namespace YoavDiscordServer
{
    public class TcpConnectionHandler
    {
        /// <summary>
        /// Length of the current message being read from the client.
        /// </summary>
        private int messageLength = -1;

        /// <summary>
        /// Total number of bytes read from the current message.
        /// </summary>
        private int totalBytesRead = 0;


        /// <summary>
        /// Indicates if the first message has been received from the client.
        /// </summary>
        private bool _isFirstMessage = true;


        /// <summary>
        /// Buffer for reading data from the client.
        /// </summary>
        private byte[] _data;

        /// <summary>
        /// Memory stream used for building incoming message data.
        /// </summary>
        private MemoryStream memoryStream = new MemoryStream();


        /// <summary>
        /// The TCP client representing the connection.
        /// </summary>
        private TcpClient _client;

        private DiscordClientConnection _discordClientConnection;

        private AesFunctions _aesFunctions;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <param name="discordClientConnection"></param>
        /// <param name="aesFunctions"></param>
        public TcpConnectionHandler(TcpClient tcpClient, DiscordClientConnection discordClientConnection, AesFunctions aesFunctions)
        {
            this._client = tcpClient;
            this._discordClientConnection = discordClientConnection;
            // Read data from the client asynchronously
            this._data = new byte[this._client.ReceiveBufferSize];
            this._aesFunctions = aesFunctions;

        }

        /// <summary>
        /// Start listen
        /// </summary>
        public void StartListen()
        {
            // Begin async read from the NetworkStream and everytime I start to get a TCP message the ReceiveMessage is called using a different thread
            // The BeginRead function registers to receive messages from the given Socket connection and whenever there is a new message ready,
            // the OS notifies the C# process about it, and a thread from the thread pool is being used to call the ReceiveMessage function 
            _client.GetStream().BeginRead(
                this._data,                   // Buffer to store received data
                0,                            // Start position in buffer
                _client.ReceiveBufferSize,    // Maximum bytes to read
                ReceiveMessage,               // CALLBACK FUNCTION - called when data arrives
                _client.GetStream()           // The NetworkStream itself - two-way communication channel
            );

            // example of OS mapping after BeginRead function is registered:
            //Socket Handle 12345 → Callback: ReceiveMessage, Object: this TcpConnectionHandler
            //Socket Handle 12346 → Callback: ReceiveMessage, Object: different TcpConnectionHandler

        }



        /// <summary>
        /// The function convert the string that we want to send to the server into byte array and send it to the server over the tcp connection
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message)
        {
            try
            {
                // send message to the server

                NetworkStream ns; // gives us access to the data that is sent and received in this connection

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
                    message = this._aesFunctions.Encrypt(message);
                }

                byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
                byte[] length = BitConverter.GetBytes(data.Length); // the data length
                byte[] bytes = new byte[data.Length + 4];
                // combine byte array, I took this code from the website StackOverFlow in this link:
                // https://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c-sharp
                System.Buffer.BlockCopy(length, 0, bytes, 0, length.Length);
                System.Buffer.BlockCopy(data, 0, bytes, length.Length, data.Length);

                // send the text
                ns.Write(bytes, 0, bytes.Length);
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
                    HandleReceivedMessage(bytesRead);
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
                stream.Close();
                this._discordClientConnection.CleanUpConnection();
            }
        }

        /// <summary>
        /// Handles incoming data from the network stream, reconstructing messages based on a 4-byte length-prefixed protocol.
        /// Supports processing of complete messages and recursively handles additional messages if multiple are received in a single read.
        /// </summary>
        /// <param name="bytesRead">The number of bytes read from the network stream during the current receive operation.</param>
        private void HandleReceivedMessage(int bytesRead)
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
                    // Reset memory stream so it can save the rest of the message
                    this.memoryStream.SetLength(0);
                }

                // If there’s more data in this function call, process it as part of the message body
                if (bytesRead > bytesToCopy)
                {
                    this.memoryStream.Write(this._data, bytesToCopy, bytesRead - bytesToCopy);
                    this.totalBytesRead += bytesRead - bytesToCopy;
                }
            }
            else
            {
                // Accumulate message data into memory stream
                this.memoryStream.Write(this._data, this.totalBytesRead, bytesRead);
                this.totalBytesRead += bytesRead;
            }

            // If we read the full message, process it
            if (this.messageLength > 0 && this.totalBytesRead >= this.messageLength + 4)
            {
                this._discordClientConnection.ProcessMessage(this.memoryStream.ToArray(), this.messageLength,
                    this._isFirstMessage);
                this._isFirstMessage = false;

                //At this point the memory stream can contain the next message
                int remainingDataBytes = this.totalBytesRead - (this.messageLength + 4);


                // Reset properties for the next message
                this.messageLength = -1;
                this.totalBytesRead = 0;
                this.memoryStream.SetLength(0);

                if(remainingDataBytes > 0) //Handle the next message, if we have one
                {
                    byte[] newData = new byte[this._client.ReceiveBufferSize];
                    Array.Copy(this._data, bytesRead - remainingDataBytes, newData, 0, remainingDataBytes);
                    this._data = newData;
                    HandleReceivedMessage(remainingDataBytes);
                }
            }
        }

        

        
    }
}
