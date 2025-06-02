using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace YoavDiscordServer
{
    /// <summary>
    /// REST client for communicating with MongoDB-backed API server for chat messages.
    /// REST = Representational State Transfer - HTTP-based communication using GET/POST methods.
    /// </summary>
    public class MongoDBRestClient
    {
        /// <summary>
        /// HTTP client for making API requests
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Base URL for the MongoDB REST API
        /// </summary>
        private readonly static string _baseApiUrl = "http://13.60.85.137";

        /// <summary>
        /// Initializes a new MongoDBRestClient with HttpClient
        /// </summary>
        public MongoDBRestClient()
        {
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Gets all messages from a specific chat room
        /// </summary>
        /// <param name="chatRoomId">ID of the chat room</param>
        /// <returns>List of messages in the chat room</returns>
        public async Task<List<UserMessage>> GetAllMessageOfChatRoom(int chatRoomId)
        {
            var url = $"{_baseApiUrl}/api/messages/{chatRoomId}";
            var response = await _httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<List<UserMessage>>(response);
        }

        /// <summary>
        /// Inserts a new message into the database
        /// </summary>
        /// <param name="newMessage">Message to insert (Id and Time are set by server)</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> InsertMessage(UserMessage newMessage)
        {
            var url = $"{_baseApiUrl}/api/messages";
            // We don't send Id or Time (Time is set by server)
            var payload = new
            {
                userId = newMessage.userId,
                username = newMessage.Username,
                message = newMessage.Message,
                chatRoomId = newMessage.ChatRoomId
            };
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            return response.IsSuccessStatusCode;
        }

        // the python code to create http api between the discord server and the monogo DB
        /*
        from flask import Flask, request, jsonify
        from pymongo import MongoClient
        from bson.objectid import ObjectId
        from datetime import datetime
        app = Flask(__name__)
        # MongoDB configuration
        client = MongoClient("mongodb://admin:Qwer!234@localhost:27017/admin?authSource=admin&ssl=false")
        db = client["admin"]
        collection = db["UserMessages"]
        @app.route('/api/messages/<int:chat_room_id>', methods=['GET'])
        def get_messages(chat_room_id):
            messages = list(collection.find({"chatRoomId": chat_room_id}).sort("Time", 1))
            for msg in messages:
                msg["_id"] = str(msg["_id"])
                msg["Time"] = msg["Time"].isoformat()
            return jsonify(messages), 200
        @app.route('/api/messages', methods=['POST'])
        def post_message():
            data = request.get_json()
            required_fields = {"userId", "username", "message", "chatRoomId"}
            if not data or not required_fields.issubset(data):
                return jsonify({"error": "Missing required fields"}), 400
            data["Time"] = datetime.utcnow()
            result = collection.insert_one(data)
            return jsonify({"message": "Message added", "id": str(result.inserted_id)}), 201
        if **name** == '__main__':
            app.run(host='0.0.0.0', port=80)


        */
    }
}