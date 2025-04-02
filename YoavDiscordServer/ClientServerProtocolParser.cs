using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoavDiscordServer
{
    public static class ClientServerProtocolParser
    {
        public const string MessageTrailingDelimiter = "vvbeclkuujdtfkktdjnktkucgdtjitckvllgtevvhicj";
        public static ClientServerProtocol Parse(string message)
        {
            return JsonConvert.DeserializeObject<ClientServerProtocol>(message);
        }

        public static string Generate(ClientServerProtocol protocol)
        {
            return JsonConvert.SerializeObject(protocol) + MessageTrailingDelimiter;
        }
    }
}
