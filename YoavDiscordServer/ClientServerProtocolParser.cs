using Newtonsoft.Json;
using System;

namespace YoavDiscordServer
{
    /// <summary>
    /// Provides methods for serializing and deserializing <see cref="ClientServerProtocol"/> objects to and from JSON,
    /// with a special trailing delimiter to indicate message boundaries.
    /// </summary>
    public static class ClientServerProtocolParser
    {
        /// <summary>
        /// A unique delimiter used to signify the end of a protocol message.
        /// </summary>
        public const string MessageTrailingDelimiter = "vvbeclkuujdtfkktdjnktkucgdtjitckvllgtevvhicj";

        /// <summary>
        /// Deserializes a JSON string into a <see cref="ClientServerProtocol"/> object.
        /// </summary>
        /// <param name="message">The JSON string representing a <see cref="ClientServerProtocol"/> object.</param>
        /// <returns>The deserialized <see cref="ClientServerProtocol"/> instance.</returns>
        public static ClientServerProtocol Parse(string message)
        {
            return JsonConvert.DeserializeObject<ClientServerProtocol>(message);
        }

        /// <summary>
        /// Serializes a <see cref="ClientServerProtocol"/> object to a JSON string and appends a unique message delimiter.
        /// </summary>
        /// <param name="protocol">The <see cref="ClientServerProtocol"/> object to serialize.</param>
        /// <returns>A JSON string representation of the protocol object with a trailing delimiter.</returns>
        public static string Generate(ClientServerProtocol protocol)
        {
            return JsonConvert.SerializeObject(protocol) + MessageTrailingDelimiter;
        }
    }
}
