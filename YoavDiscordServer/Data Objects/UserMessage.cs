using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace YoavDiscordServer
{
    /// <summary>
    /// Represents a message sent by a user in a chat room.
    /// This class is used for storing and retrieving messages from MongoDB.
    /// </summary>
    [Serializable]
    public class UserMessage
    {
        /// <summary>
        /// Gets or sets the unique identifier for the message (MongoDB ObjectId).
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who sent the message.
        /// </summary>
        [BsonElement("userId")]
        public int userId { get; set; }

        /// <summary>
        /// Gets or sets the username of the user who sent the message.
        /// </summary>
        [BsonElement("username")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the content of the message.
        /// </summary>
        [BsonElement("message")]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the time the message was sent, stored in UTC.
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the ID of the chat room in which the message was sent.
        /// </summary>
        [BsonElement("chatRoomId")]
        public int ChatRoomId { get; set; }
    }
}
