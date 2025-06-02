using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace YoavDiscordServer
{
    /// <summary>
    /// Provides a singleton client for MongoDB database operations.
    /// Handles connections to the MongoDB server and manages message data for the Discord server clone.
    /// 
    /// NOTE: This class is not used anymore because the firewall in my classroom probably blocks the connection to this port (27017)
    /// </summary>
    public class MongoDBClient
    {
        /// <summary>
        /// Singleton instance of the MongoDBClient.
        /// </summary>
        private static MongoDBClient _client = null;

        /// <summary>
        /// The MongoDB client connection used to interact with the MongoDB server.
        /// </summary>
        private MongoClient mongoClient;

        /// <summary>
        /// Reference to the MongoDB database instance.
        /// </summary>
        private IMongoDatabase database;

        /// <summary>
        /// The name of the MongoDB database to connect to.
        /// </summary>
        private const string DATA__BASE_NAME = "admin";

        /// <summary>
        /// The IP address of the MongoDB server.
        /// </summary>
        private const string DATA_BASE_IP = "13.60.85.137";

        /// <summary>
        /// The name of the collection where user messages are stored.
        /// </summary>
        private const string COLLECTION_NAME = "UserMessages";

        /// <summary>
        /// Gets the singleton instance of the MongoDBClient.
        /// If the instance doesn't exist, it creates one.
        /// This method is thread-safe due to the Synchronized attribute.
        /// </summary>
        /// <returns>The singleton instance of MongoDBClient</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static MongoDBClient GetInstance()
        {
            if (_client == null)
            {
                _client = new MongoDBClient();
            }
            return _client;
        }

        /// <summary>
        /// Private constructor to enforce the singleton pattern.
        /// Initializes the MongoDB connection using the predefined connection parameters.
        /// </summary>
        private MongoDBClient()
        {
            string connectionString = $"mongodb://admin:Qwer!234@{DATA_BASE_IP}:27017/{DATA__BASE_NAME}?authSource=admin&ssl=false";
            this.mongoClient = new MongoClient(connectionString);
            this.database = mongoClient.GetDatabase(DATA__BASE_NAME);
        }

        /// <summary>
        /// Inserts a new user message into the MongoDB collection.
        /// </summary>
        /// <param name="userMessage">The UserMessage object to insert into the database</param>
        public async void InsertMessage(UserMessage userMessage)
        {
            var collection = database.GetCollection<UserMessage>(COLLECTION_NAME);
            await collection.InsertOneAsync(userMessage);
        }

        /// <summary>
        /// Retrieves all messages from a specific chat room.
        /// Messages are sorted in ascending order by the time they were sent.
        /// </summary>
        /// <param name="chatRoomId">The ID of the chat room to get messages from</param>
        /// <returns>A task that resolves to a list of UserMessage objects for the specified chat room</returns>
        public async Task<List<UserMessage>> GetAllMessageOfChatRoom(int chatRoomId)
        {
            // example:
            //MongoDB Server
            //└── Database: admin
            //    └── Collection: UserMessages
            //        ├── Document 1
            //        │   ├── _id: ObjectId("...1")
            //        │   ├── userId: 101
            //        │   ├── username: "alice"
            //        │   ├── message: "Hello, world!"
            //        │   ├── time: 2025 - 04 - 18T10: 00:00Z
            //        │   └── chatRoomId: 1
            var collection = database.GetCollection<UserMessage>(COLLECTION_NAME);
            var filter = Builders<UserMessage>.Filter.Eq(e => e.ChatRoomId, chatRoomId);
            var sort = Builders<UserMessage>.Sort.Ascending(e => e.Time); // the first message will be the oldest message
            return await collection.Find(filter).Sort(sort).ToListAsync();
        }
    }
}